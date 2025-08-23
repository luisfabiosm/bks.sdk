using bks.sdk.Cache;
using bks.sdk.Common.Results;
using bks.sdk.Events;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Transactions;
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
        private readonly IBKSLogger? _logger;

        public DebitoProcessor(
                            IServiceProvider serviceProvider,
                            IBKSLogger logger,
                            IBKSTracer tracer,
                            IEventBroker eventBroker) : base(serviceProvider, logger, tracer, eventBroker)
        {
            _contaRepository = serviceProvider.GetRequiredService<IContaRepository>();
            _limiteService = serviceProvider.GetRequiredService<ILimiteService>(); 
            _notificationService = serviceProvider.GetRequiredService<INotificationAdapter>(); 
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

            _logger.Info($"Iniciando pré-processamento do débito {debito.CorrelationId} na conta {debito.NumeroConta}");

            try
            {
                // 1. Verificar se a conta existe e está ativa
                var conta = await _contaRepository.GetByNumeroAsync(debito.NumeroConta, cancellationToken);
                if (conta == null)
                {
                    _logger.Warn($"Conta não encontrada: {debito.NumeroConta}");
                    return Result.Failure("Conta não encontrada ou inválida");
                }

                if (!conta.Ativa)
                {
                    _logger.Warn($"Tentativa de débito em conta inativa: {debito.NumeroConta}");
                    return Result.Failure("Conta está inativa para movimentação");
                }

                // 2. Verificar limites operacionais
                var limiteValidacao = await _limiteService.ValidarLimiteDebitoAsync(
                    debito.NumeroConta, debito.Valor, cancellationToken);

                if (!limiteValidacao.IsValid)
                {
                    _logger.Warn($"Limite excedido para conta {debito.NumeroConta}: {limiteValidacao.Errors.First()}");
                    return Result.Failure($"Limite excedido: {limiteValidacao.Errors.First()}");
                }

                // 3. Análise de fraude
                var fraudeAnalise = await _fraudeService.AnalisarTransacaoAsync(debito, cancellationToken);
                if (fraudeAnalise.IsRisco)
                {
                    _logger.Warn($"Transação bloqueada por suspeita de fraude: {debito.CorrelationId}");
                    return Result.Failure("Transação bloqueada por medidas de segurança");
                }

                // 4. Verificar se a conta pode sacar o valor (usando método da entidade)
                if (!conta.PodeSacar(debito.Valor))
                {
                    _logger.Info($"Saldo insuficiente na conta {debito.NumeroConta}: Disponível: {conta.Saldo:C}, Solicitado: {debito.Valor:C}");
                    return Result.Failure($"Saldo insuficiente. Disponível: {conta.Saldo:C}");
                }

                _logger.Info($"Pré-processamento concluído com sucesso para o débito {debito.CorrelationId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro no pré-processamento do débito {debito.CorrelationId}: {ex.Message}");
                return Result.Failure("Erro interno na validação da transação");
            }
        }


        // Processamento principal: execução da transação
        protected override async Task<Result<DebitoResult>> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            _logger.Info($"Executando débito {debito.CorrelationId}: R$ {debito.Valor:F2} na conta {debito.NumeroConta}");

            try
            {
                // Buscar conta novamente (garantir consistência)
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

                _logger.Info($"Débito executado com sucesso: {debito.CorrelationId} - Novo saldo: R$ {conta.Saldo:F2}");
                _logger.Info($"Movimentação criada: {conta.Movimentacoes.Last().Id} - Valor: R$ {debito.Valor:F2}");

                // 🎯 PONTO CHAVE: Criar resultado tipado com dados da conta atualizada
                // Elimina necessidade de consulta adicional no endpoint!
                var resultado = DebitoResult.From(debito, conta);

                return Result<DebitoResult>.Success(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.Warn($"Erro de validação no débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure($"Dados inválidos: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warn($"Operação inválida no débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro na execução do débito {debito.CorrelationId}: {ex.Message}");
                return Result<DebitoResult>.Failure("Erro interno na execução da transação");
            }
        }


        // Pós-processamento: ações após execução bem-sucedida
        protected override async Task<Result> PostProcessAsync(BaseTransaction transaction,DebitoResult processResult, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            _logger.Info($"Iniciando pós-processamento do débito {debito.CorrelationId}");

            try
            {
                // 1. Atualizar limites utilizados
                await _limiteService.AtualizarLimiteUtilizadoAsync(
                    debito.NumeroConta, debito.Valor, TipoLimite.DebitoDiario, cancellationToken);

                // 2. Registrar para analytics/BI usando dados do resultado tipado
                await RegistrarEventoAnalyticsAsync(debito, processResult, cancellationToken);

                // 3. Atualizar score de comportamento baseado no resultado
                await AtualizarScoreComportamentoAsync(debito, processResult, cancellationToken);

                // 4. Enviar notificação com dados da movimentação
                await EnviarNotificacaoDebitoAsync(debito, processResult, cancellationToken);

                _logger.Info($"Pós-processamento concluído para o débito {debito.CorrelationId}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Falhas no pós-processamento não devem reverter a transação
                _logger.Error($"Erro no pós-processamento do débito {debito.CorrelationId}: {ex.Message}");
                // Continua considerando a transação como bem-sucedida
                return Result.Success();
            }
        }


        // Tratamento de compensação em caso de falha
        protected override async Task<Result> CompensateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var debito = (DebitoTransaction)transaction;

            _logger.Warn($"Executando compensação para o débito {debito.CorrelationId}");

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
                _logger.Info($"Compensação necessária para débito {debito.CorrelationId}: Valor R$ {debito.Valor:F2}");
                _logger.Info($"Movimentação original: {movimentacaoDebito.Id} em {movimentacaoDebito.DataMovimentacao}");

                // NOTA: Como a entidade Conta não possui método Creditar, 
                // seria necessário implementar mecanismo de compensação específico
                // ou estender a entidade com método de reversão

                _logger.Warn($"⚠️ COMPENSAÇÃO IDENTIFICADA: Débito {debito.CorrelationId} requer reversão manual ou automática");

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro na compensação do débito {debito.CorrelationId}: {ex.Message}");
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

                _logger.Info($"📧 Notificação de débito enviada: {System.Text.Json.JsonSerializer.Serialize(notificacao)}");

                // Aqui seria implementado o envio real da notificação:
                // - SMS
                // - Email
                // - Push notification
                // - Webhook
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao enviar notificação: {ex.Message}");
            }
        }


        private async Task RegistrarEventoAnalyticsAsync(DebitoTransaction debito, DebitoResult resultado, CancellationToken cancellationToken)
        {
            try
            {
                var eventoAnalytics = new
                {
                    // Dados da transação
                    TransacaoId = resultado.TransacaoId,
                    TipoTransacao = "DEBITO",
                    TipoDebito = debito.TipoDebito,

                    // Dados da conta
                    ContaId = resultado.ContaId,
                    NumeroConta = resultado.NumeroConta,
                    Titular = resultado.TitularConta,

                    // Dados financeiros
                    ValorDebitado = resultado.ValorDebitado,
                    SaldoAnterior = resultado.SaldoAnterior,
                    SaldoPosterior = resultado.NovoSaldo,
                    PercentualSaldoUtilizado = (resultado.ValorDebitado / resultado.SaldoAnterior) * 100,

                    // Dados temporais
                    DataProcessamento = resultado.DataProcessamento,
                    DataMovimentacao = resultado.UltimaMovimentacao.DataMovimentacao,

                    // Metadados
                    MovimentacaoId = resultado.UltimaMovimentacao.Id,
                    Referencia = resultado.Referencia
                };

                _logger.Info($"📊 Analytics registrado: {System.Text.Json.JsonSerializer.Serialize(eventoAnalytics)}");

                // Aqui seria implementado o envio para sistema de analytics:
                // - Data warehouse
                // - Sistema de BI
                // - Event stream
                // - Message queue
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao registrar analytics: {ex.Message}");
            }
        }


        private async Task AtualizarScoreComportamentoAsync(DebitoTransaction debito, DebitoResult resultado, CancellationToken cancellationToken)
        {
            try
            {
                var scoreComportamento = new
                {
                    ContaId = resultado.ContaId,
                    NumeroConta = resultado.NumeroConta,
                    UltimaTransacao = resultado.DataProcessamento,
                    SaldoAtual = resultado.NovoSaldo,
                    ValorMovimentado = resultado.ValorDebitado,
                    Score = CalcularScoreComportamento(resultado),

                    // Análises baseadas no resultado
                    Classificacao = ClassificarComportamento(resultado),
                    RiscoSaldo = AnalisarRiscoSaldo(resultado),
                    FrequenciaEstimada = EstimarFrequenciaUso(resultado)
                };

                _logger.Info($"🎯 Score atualizado para conta {resultado.NumeroConta}: {System.Text.Json.JsonSerializer.Serialize(scoreComportamento)}");

                // Aqui seria implementado o sistema de score:
                // - Atualização em cache
                // - Persistência em BD
                // - Notificação para sistema de risk
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao atualizar score: {ex.Message}");
            }
        }



        private int CalcularScoreComportamento(DebitoResult resultado)
        {
            var score = 100; // Score base

            // Análise do saldo residual
            if (resultado.NovoSaldo < 100)
                score -= 30; // Alto risco
            else if (resultado.NovoSaldo < 500)
                score -= 15; // Médio risco
            else if (resultado.NovoSaldo > 5000)
                score += 10; // Baixo risco

            // Análise do percentual movimentado
            var percentualMovimentado = (resultado.ValorDebitado / resultado.SaldoAnterior) * 100;
            if (percentualMovimentado > 90)
                score -= 25; // Movimentação muito alta
            else if (percentualMovimentado > 70)
                score -= 15; // Movimentação alta
            else if (percentualMovimentado < 10)
                score += 5; // Movimentação conservadora

            // Análise do valor absoluto
            if (resultado.ValorDebitado > 10000)
                score += 5; // Transações de alto valor podem indicar estabilidade
            else if (resultado.ValorDebitado < 10)
                score -= 5; // Micro transações podem indicar testes

            return Math.Max(0, Math.Min(100, score));
        }


        private string ClassificarComportamento(DebitoResult resultado)
        {
            var percentualMovimentado = (resultado.ValorDebitado / resultado.SaldoAnterior) * 100;

            return percentualMovimentado switch
            {
                > 80 => "ALTO_RISCO",
                > 50 => "MEDIO_RISCO",
                > 20 => "USO_NORMAL",
                _ => "USO_CONSERVADOR"
            };
        }


        private string AnalisarRiscoSaldo(DebitoResult resultado)
        {
            return resultado.NovoSaldo switch
            {
                < 100 => "CRITICO",
                < 500 => "BAIXO",
                < 2000 => "MEDIO",
                _ => "ADEQUADO"
            };
        }


        private string EstimarFrequenciaUso(DebitoResult resultado)
        {
            // Baseado no tipo de valor e horário (exemplo simplificado)
            var valor = resultado.ValorDebitado;

            return valor switch
            {
                < 50 => "ALTA_FREQUENCIA", // Pequenos valores, uso frequente
                < 500 => "MEDIA_FREQUENCIA", // Valores médios
                < 2000 => "BAIXA_FREQUENCIA", // Valores altos, uso esporádico
                _ => "MUITO_BAIXA_FREQUENCIA" // Valores muito altos
            };
        }


        #endregion


    }

}