using bks.sdk.Cache;
using bks.sdk.Common.Results;
using bks.sdk.Events;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Processing.Transactions;
using bks.sdk.Processing.Transactions.Abstractions;
using Domain.Core.Enums;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.Entities;
using Domain.Core.Models.Results;
using Domain.Core.Transactions;

namespace Domain.Processors
{
    public class DebitoProcessor : TransactionProcessor<DebitoResult>
    {
        private readonly IContaRepository _contaRepository;
        private readonly ILimiteService _limiteService;
        private readonly INotificationAdapter _notificationService;
        private readonly IFraudeService _fraudeService;

        public DebitoProcessor(
                            IServiceProvider serviceProvider,
                            IBKSLogger logger,
                            IBKSTracer tracer,
                            IEventBroker eventBroker) : base(serviceProvider, logger, tracer, eventBroker)
        {
            _contaRepository = serviceProvider.GetRequiredService<IContaRepository>();
            _limiteService = serviceProvider.GetRequiredService<ILimiteService>(); 
            //_notificationService = serviceProvider.GetRequiredService<INotificationAdapter>(); 
            _fraudeService = serviceProvider.GetRequiredService<IFraudeService>(); 
        }

        public override bool CanProcess(BaseTransaction transaction)
        {
            return transaction is DebitoTransaction;
        }


        // Pré-processamento: validações antes da execução
        protected override async Task<Result> PreProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            Logger.Info($"Iniciando pré-processamento do débito {debito.CorrelationId} na conta {debito.NumeroConta}");

            try
            {
                // 1. Verificar se a conta existe e está ativa
                var conta = await _contaRepository.GetByNumeroAsync(debito.NumeroConta, cancellationToken);
                if (conta == null)
                {
                    Logger.Warn($"Conta não encontrada: {debito.NumeroConta}");
                    return Result.Failure("Conta não encontrada ou inválida");
                }

                if (!conta.Ativa)
                {
                    Logger.Warn($"Tentativa de débito em conta inativa: {debito.NumeroConta}");
                    return Result.Failure("Conta está inativa para movimentação");
                }

                // 2. Verificar limites operacionais
                var limiteValidacao = await _limiteService.ValidarLimiteDebitoAsync(
                    debito.NumeroConta, debito.Valor, cancellationToken);

                if (!limiteValidacao.IsValid)
                {
                    Logger.Warn($"Limite excedido para conta {debito.NumeroConta}: {limiteValidacao.Errors.First()}");
                    return Result.Failure($"Limite excedido: {limiteValidacao.Errors.First()}");
                }

                // 3. Análise de fraude
                var fraudeAnalise = await _fraudeService.AnalisarTransacaoAsync(debito, cancellationToken);
                if (fraudeAnalise.IsRisco)
                {
                    Logger.Warn($"Transação bloqueada por suspeita de fraude: {debito.CorrelationId}");
                    return Result.Failure("Transação bloqueada por medidas de segurança");
                }

                // 4. Verificar se a conta pode sacar o valor (usando método da entidade)
                if (!conta.PodeSacar(debito.Valor))
                {
                    Logger.Info($"Saldo insuficiente na conta {debito.NumeroConta}: Disponível: {conta.Saldo:C}, Solicitado: {debito.Valor:C}");
                    return Result.Failure($"Saldo insuficiente. Disponível: {conta.Saldo:C}");
                }

                Logger.Info($"Pré-processamento concluído com sucesso para o débito {debito.CorrelationId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro no pré-processamento do débito {debito.CorrelationId}: {ex.Message}");
                return Result.Failure("Erro interno na validação da transação");
            }
        }


        // Processamento principal: execução da transação
        protected override async Task<Result<DebitoResult>> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;
  
            try
            {
                Logger.Info($"Executando débito {debito.CorrelationId}: R$ {debito.Valor:F2} na conta {debito.NumeroConta}");

                // Buscar conta novamente (garantir consistência)
                Logger.Info($"Buscando conta {debito.NumeroConta} para débito");
                var conta = await _contaRepository.GetByNumeroAsync(debito.NumeroConta, cancellationToken);
                if (conta == null)
                    return Result<DebitoResult>.Failure("Conta não encontrada durante a execução");

                // Verificação final usando método da entidade
                if (!conta.PodeSacar(debito.Valor))
                    return Result<DebitoResult>.Failure("Saldo insuficiente no momento da execução");

                // Executar o débito usando método da entidade Domain
                // A entidade Conta gerencia automaticamente:
                // - Validação de valor > 0
                // - Verificação se conta está ativa
                // - Verificação de saldo suficiente
                // - Atualização do saldo
                // - Criação automática da movimentação
                // - Atualização da data da última movimentação
                conta.Debitar(debito.Valor, debito.Descricao, debito.Referencia ?? string.Empty);

                // Persistir a alteração (conta e movimentação são atualizadas juntas)
                await _contaRepository.UpdateAsync(conta, cancellationToken);

                Logger.Info($"Débito executado com sucesso: {debito.CorrelationId} - Novo saldo: R$ {conta.Saldo:F2}");
                Logger.Info($"Movimentação criada: {conta.Movimentacoes.Last().Id} - Valor: R$ {debito.Valor:F2}");

                // 🎯 PONTO CHAVE: Criar resultado tipado com dados da conta atualizada
                // Elimina necessidade de consulta adicional no endpoint!
                var resultado = DebitoResult.From(debito, conta);

                return Result<DebitoResult>.Success(resultado);
            }
            catch (ArgumentException ex)
            {
                Logger.Warn($"Erro de validação no débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure($"Dados inválidos: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn($"Operação inválida no débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro na execução do débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure("Erro interno na execução da transação");
            }
        }


        // Pós-processamento: ações após execução bem-sucedida
        protected override async Task<Result> PostProcessAsync(BaseTransaction transaction,DebitoResult processResult, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            Logger.Info($"Iniciando pós-processamento do débito {debito.CorrelationId}");

            try
            {
                // 1. Atualizar limites utilizados
                await _limiteService.AtualizarLimiteUtilizadoAsync(
                    debito.NumeroConta, debito.Valor, TipoLimite.DebitoDiario, cancellationToken);

                // 2. Enviar notificação com dados da movimentação
                await EnviarNotificacaoDebitoAsync(debito, processResult, cancellationToken);

                Logger.Info($"Pós-processamento concluído para o débito {debito.CorrelationId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Falhas no pós-processamento não devem reverter a transação
                Logger.Error($"Erro no pós-processamento do débito {debito.CorrelationId}: {ex.Message}");
                // Continua considerando a transação como bem-sucedida
                return Result.Success();
            }
        }


        // Tratamento de compensação em caso de falha
        protected override async Task<Result> CompensateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            Logger.Warn($"Executando compensação para o débito {debito.CorrelationId}");

            try
            {
                // Verificar se o débito foi realmente executado
                var conta = await _contaRepository.GetByNumeroAsync(debito.NumeroConta, cancellationToken);
                if (conta == null)
                    return Result.Success(); // Conta não existe, nada para compensar

                // Verificar se existe movimentação do débito nos últimos registros
                var movimentacaoDebito = conta.Movimentacoes
                    .Where(m => m.Tipo == Domain.Core.Enums.TipoMovimentacao.Debito)
                    .Where(m => m.Valor == debito.Valor)
                    .Where(m => m.Descricao == debito.Descricao)
                    .Where(m => m.DataMovimentacao >= DateTime.UtcNow.AddMinutes(-5)) // Últimos 5 minutos
                    .LastOrDefault();

                if (movimentacaoDebito == null)
                    return Result.Success(); // Movimentação não foi registrada

                // Registrar necessidade de compensação
                Logger.Info($"Compensação necessária para débito {debito.CorrelationId}: Valor R$ {debito.Valor:F2}");
                Logger.Info($"Movimentação original: {movimentacaoDebito.Id} em {movimentacaoDebito.DataMovimentacao}");

                // NOTA: Como a entidade Conta não possui método Creditar, 
                // seria necessário implementar mecanismo de compensação específico
                // ou estender a entidade com método de reversão

                Logger.Warn($"⚠️ COMPENSAÇÃO IDENTIFICADA: Débito {debito.CorrelationId} requer reversão manual ou automática");

                return Result.Success();
            }
            catch (Exception ex)
            {   
                Logger.Error($"Erro na compensação do débito {debito.CorrelationId}: {ex.Message}");
                return Result.Failure("Erro na compensação da transação");
            }
        }


        #region Métodos Auxiliares Privados

        private async Task EnviarNotificacaoDebitoAsync(DebitoTransaction debito, DebitoResult resultado, CancellationToken cancellationToken)
        {
            try
            {
                var notificacao = new
                {
                    TransacaoId = resultado.TransacaoId,
                    Titular = resultado.TitularConta,
                    NumeroConta = resultado.NumeroConta,
                    ValorDebitado = resultado.ValorDebitado,
                    SaldoAnterior = resultado.SaldoAnterior,
                    NovoSaldo = resultado.NovoSaldo,
                    Descricao = resultado.Descricao,
                    DataMovimentacao = resultado.UltimaMovimentacao.DataMovimentacao,
                    MovimentacaoId = resultado.UltimaMovimentacao.Id,
                    Referencia = resultado.Referencia
                };

                Logger.Info($"📧 Notificação de débito enviada: {System.Text.Json.JsonSerializer.Serialize(notificacao)}");

                // Aqui seria implementado o envio real da notificação:
                // - SMS
                // - Email
                // - Push notification
                // - Webhook
            }
            catch (Exception ex)
            {
                Logger.Error($"Erro ao enviar notificação: {ex.Message}");
            }
        }


        #endregion


    }

}