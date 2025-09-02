using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Processing.Transactions.Processors;
using Domain.Core.Entities;
using Domain.Core.Ports.Outbound;
using Domain.Core.Transactions;

namespace Domain.Processors
{
    public class DebitoProcessor : BaseTransactionProcessor<DebitoTransaction, DebitoResponse>
    {
        private readonly IContaRepository _contaRepository;
        public override string ProcessorName => "DebitoTransactionProcessor";

        public DebitoProcessor(
            IContaRepository contaRepository,
            IBKSLogger logger,
            IBKSTracer tracer) : base(logger, tracer)
        {
            _contaRepository = contaRepository;
        }

        protected override async Task<Result<DebitoResponse>> ProcessTransactionAsync(
            DebitoTransaction transaction,
            CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info($"Processando débito - Conta: {transaction.NumeroConta}, Valor: {transaction.Valor:C}");

                // 1. Validar transação
                var resultadoValidacao = await ValidarTransacaoAsync(transaction, cancellationToken);
                if (!resultadoValidacao.IsSuccess)
                {
                    return Result<DebitoResponse>.Failure(resultadoValidacao.Error!);
                }

                // 2. Buscar conta
                var conta = await _contaRepository.GetByNumeroAsync(transaction.NumeroConta, cancellationToken);
                if (conta == null)
                {
                    return Result<DebitoResponse>.Failure($"Conta não encontrada: {transaction.NumeroConta}");
                }

                // 3. Validar conta e saldo
                var resultadoValidacaoConta = await ValidarContaParaDebitoAsync(conta, transaction, cancellationToken);
                if (!resultadoValidacaoConta.IsSuccess)
                {
                    return Result<DebitoResponse>.Failure(resultadoValidacaoConta.Error!);
                }

                // 4. Executar débito
                var saldoAnterior = conta.Saldo;
                var resultadoDebito = await ExecutarDebitoAsync(conta, transaction, cancellationToken);

                if (!resultadoDebito.IsSuccess)
                {
                    return Result<DebitoResponse>.Failure(resultadoDebito.Error!);
                }

                // 5. Persistir alterações
                await _contaRepository.UpdateAsync(conta, cancellationToken);

                // 6. Criar response de sucesso
                var movimentacaoId = conta.Movimentacoes.LastOrDefault()?.Id ?? string.Empty;
                var response = CriarResponseSucesso(conta, transaction, saldoAnterior, movimentacaoId);

                Logger.Info($"Débito processado com sucesso - Conta: {transaction.NumeroConta}, " +
                           $"Valor: {transaction.Valor:C}, Novo Saldo: {conta.Saldo:C}");

                return Result<DebitoResponse>.Success(response);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Erro ao processar débito - Conta: {transaction.NumeroConta}");
                return Result<DebitoResponse>.Failure($"Erro interno no processamento: {ex.Message}");
            }
        }

        private async Task<Result> ValidarTransacaoAsync(DebitoTransaction transaction, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken); // Simula validação assíncrona

            var erros = new List<string>();

            // Validações básicas
            if (!transaction.IsValorValido)
                erros.Add($"Valor inválido: R$ {transaction.Valor:F2}. Deve ser maior que zero e menor que o limite.");

            if (!transaction.IsContaValida)
                erros.Add("Número da conta de débito é obrigatório");

            if (!transaction.IsDescricaoValida)
                erros.Add("Descrição é obrigatória e deve ter no máximo 200 caracteres");

            // Validação de horário para operações grandes
            if (transaction.Valor > 10000 && !transaction.IsHorarioComercial)
            {
                erros.Add("Operações acima de R$ 10.000,00 só podem ser realizadas em horário comercial");
            }

            // Validação de limite diário (simulada)
            if (transaction.Valor > 50000)
            {
                erros.Add("Valor excede o limite diário de transações");
            }

            if (erros.Count > 0)
            {
                var mensagem = $"Validação falhou: {string.Join("; ", erros)}";
                Logger.Warn($"Transação inválida - {mensagem}");
                return Result.Failure(mensagem);
            }

            return Result.Success();
        }

        private async Task<Result> ValidarContaParaDebitoAsync(
            Conta conta,
            DebitoTransaction transaction,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            // Verificar se conta está ativa
            if (!conta.Ativa)
            {
                Logger.Warn($"Tentativa de débito em conta inativa: {conta.Numero}");
                return Result.Failure($"Conta inativa: {conta.Numero}");
            }

            // Verificar saldo suficiente
            if (!transaction.PermitirSaldoNegativo && !conta.PodeSacar(transaction.Valor))
            {
                Logger.Warn($"Saldo insuficiente - Conta: {conta.Numero}, Saldo: {conta.Saldo:C}, Débito: {transaction.Valor:C}");
                return Result.Failure($"Saldo insuficiente. Saldo atual: {conta.Saldo:C}, Valor solicitado: {transaction.Valor:C}");
            }

            // Validações adicionais de negócio
            if (transaction.Valor > conta.Saldo * 2) // Não pode debitar mais de 2x o saldo atual
            {
                return Result.Failure("Valor do débito excede o limite permitido baseado no saldo atual");
            }

            // Verificar se não é uma conta empresarial fazendo operação fora do horário
            if (conta.Titular.Contains("Ltda", StringComparison.OrdinalIgnoreCase) &&
                !transaction.IsHorarioComercial &&
                transaction.Valor > 1000)
            {
                return Result.Failure("Operações empresariais acima de R$ 1.000,00 só podem ser realizadas em horário comercial");
            }

            return Result.Success();
        }

        private async Task<Result> ExecutarDebitoAsync(
            Conta conta,
            DebitoTransaction transaction,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(15, cancellationToken); // Simula processamento da operação

                // Aplicar regras específicas antes do débito
                var taxa = await CalcularTaxaOperacaoAsync(transaction, cancellationToken);

                if (taxa > 0)
                {
                    Logger.Info($"Taxa de operação aplicada: {taxa:C} para débito de {transaction.Valor:C}");

                    // Verificar se tem saldo para taxa também
                    if (!transaction.PermitirSaldoNegativo && conta.Saldo < (transaction.Valor + taxa))
                    {
                        return Result.Failure($"Saldo insuficiente para débito + taxa. Necessário: {(transaction.Valor + taxa):C}");
                    }
                }

                // Executar débito principal
                conta.Debitar(transaction.Valor, transaction.Descricao);

                // Executar cobrança de taxa se aplicável
                if (taxa > 0)
                {
                    conta.Debitar(taxa, $"Taxa de operação - {transaction.Descricao}");
                }

                Logger.Trace($"Débito executado - Conta: {conta.Numero}, Valor: {transaction.Valor:C}, Taxa: {taxa:C}");

                return Result.Success();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn($"Operação de débito inválida: {ex.Message}");
                return Result.Failure(ex.Message);
            }
            catch (ArgumentException ex)
            {
                Logger.Warn($"Argumentos inválidos para débito: {ex.Message}");
                return Result.Failure(ex.Message);
            }
        }

        private async Task<decimal> CalcularTaxaOperacaoAsync(DebitoTransaction transaction, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            // Simular cálculo de taxa baseado no valor e horário
            decimal taxa = 0;

            // Taxa para operações fora do horário comercial
            if (!transaction.IsHorarioComercial && transaction.Valor > 500)
            {
                taxa += 5.00m; // Taxa fixa de R$ 5,00
            }

            // Taxa para operações de alto valor
            if (transaction.Valor > 10000)
            {
                taxa += transaction.Valor * 0.001m; // 0,1% do valor
            }

            // Taxa máxima de R$ 50,00
            taxa = Math.Min(taxa, 50.00m);

            return taxa;
        }

        private DebitoResponse CriarResponseSucesso(
            Conta conta,
            DebitoTransaction transaction,
            decimal saldoAnterior,
            string movimentacaoId)
        {
            return new DebitoResponse
            {
                Sucesso = true,
                Mensagem = "Débito processado com sucesso",
                NovoSaldo = conta.Saldo,
                SaldoAnterior = saldoAnterior,
                ContaId = conta.Id,
                MovimentacaoId = movimentacaoId,
                ValorDebitado = transaction.Valor,
                DataProcessamento = DateTime.UtcNow
            };
        }

        protected override Task OnProcessing(DebitoTransaction transaction)
        {
            Logger.Info($"Iniciando processamento de débito - TransactionId: {transaction.Id}");

            // Adicionar informações à transação para auditoria
            transaction.Metadata["ProcessorName"] = ProcessorName;
            transaction.Metadata["ProcessingStartedAt"] = DateTime.UtcNow.ToString("O");

            return Task.CompletedTask;
        }

        protected override Task OnProcessed(DebitoTransaction transaction, Result<DebitoResponse> result)
        {
            if (result.IsSuccess)
            {
                Logger.Info($"Débito processado com sucesso - TransactionId: {transaction.Id}");
                transaction.Metadata["ProcessingCompletedAt"] = DateTime.UtcNow.ToString("O");
            }
            return Task.CompletedTask;
        }

        protected override Task OnFailed(DebitoTransaction transaction, Result<DebitoResponse> result)
        {
            Logger.Warn($"Falha no processamento de débito - TransactionId: {transaction.Id}, Erro: {result.Error}");
            transaction.Metadata["ProcessingFailedAt"] = DateTime.UtcNow.ToString("O");
            transaction.Metadata["FailureReason"] = result.Error ?? "Erro desconhecido";
            return Task.CompletedTask;
        }

        protected override Task OnException(DebitoTransaction transaction, Exception exception)
        {
            Logger.Error(exception, $"Exceção no processamento de débito - TransactionId: {transaction.Id}");
            transaction.Metadata["ExceptionOccurredAt"] = DateTime.UtcNow.ToString("O");
            transaction.Metadata["ExceptionMessage"] = exception.Message;
            transaction.Metadata["ExceptionType"] = exception.GetType().Name;
            return Task.CompletedTask;
        }

    }
}
