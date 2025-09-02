using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Processing.Mediator.Handlers;
using Domain.Core.Commands;
using Domain.Core.Entities;
using Domain.Core.Ports.Outbound;

namespace Domain.UseCases
{
    public class UseCaseProcessarCreditoHandler : BaseRequestHandler<ProcessarCreditoCommand, ProcessarCreditoResponse>
    {
        private readonly IContaRepository _contaRepository;

        public UseCaseProcessarCreditoHandler(
            IContaRepository contaRepository,
            IBKSLogger logger,
            IBKSTracer tracer) : base(logger, tracer)
        {
            _contaRepository = contaRepository;
        }

        protected override async Task<Result<ProcessarCreditoResponse>> ProcessAsync(
            ProcessarCreditoCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                Logger.Info($"Iniciando processamento de crédito - Conta: {request.NumeroContaCredito}, Valor: {request.Valor:C}");

                // 1. Validar comando
                var resultadoValidacao = await ValidarComandoAsync(request, cancellationToken);
                if (!resultadoValidacao.IsSuccess)
                {
                    return resultadoValidacao;
                }

                // 2. Buscar conta
                var conta = await _contaRepository.GetByNumeroAsync(request.NumeroContaCredito, cancellationToken);
                if (conta == null)
                {
                    return Result<ProcessarCreditoResponse>.Failure($"Conta não encontrada: {request.NumeroContaCredito}");
                }

                // 3. Verificar se conta está ativa
                if (!conta.Ativa)
                {
                    return Result<ProcessarCreditoResponse>.Failure($"Conta inativa: {request.NumeroContaCredito}");
                }

                // 4. Executar crédito
                var saldoAnterior = conta.Saldo;
                var resultado = await ExecutarCreditoAsync(conta, request, cancellationToken);

                if (!resultado.IsSuccess)
                {
                    return resultado;
                }

                // 5. Persistir alterações
                await _contaRepository.UpdateAsync(conta, cancellationToken);

                // 6. Criar response de sucesso
                var movimentacaoId = conta.Movimentacoes.LastOrDefault()?.Id;
                var response = ProcessarCreditoResponse.Concluido(conta, request.Valor, saldoAnterior, movimentacaoId);

                // 7. Log de sucesso
                Logger.Info($"Crédito processado com sucesso - Conta: {request.NumeroContaCredito}, " +
                           $"Valor: {request.Valor:C}, Novo Saldo: {conta.Saldo:C}");

                // 8. Adicionar dados adicionais para auditoria
                response.DadosAdicionais["CorrelationId"] = request.CorrelationId;
                response.DadosAdicionais["RequestId"] = request.RequestId;
                response.DadosAdicionais["ProcessedBy"] = "Mediator Pattern";

                return Result<ProcessarCreditoResponse>.Success(response);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Erro ao processar crédito - Conta: {request.NumeroContaCredito}");
                return Result<ProcessarCreditoResponse>.Failure($"Erro interno: {ex.Message}");
            }
        }

        private async Task<Result<ProcessarCreditoResponse>> ValidarComandoAsync(
            ProcessarCreditoCommand request,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // Placeholder para validações assíncronas futuras

            if (!request.IsValido)
            {
                var erros = request.ObterErrosValidacao();
                var mensagem = $"Dados inválidos: {string.Join(", ", erros)}";

                Logger.Warn($"Validação falhou para crédito - {mensagem}");

                return Result<ProcessarCreditoResponse>.Failure(mensagem);
            }

            // Validações adicionais de negócio
            if (await ValidarHorarioOperacaoAsync(cancellationToken))
            {
                return Result<ProcessarCreditoResponse>.Failure("Operação fora do horário comercial");
            }

            return Result<ProcessarCreditoResponse>.Success(null!); // Validação passou
        }

        private async Task<Result<ProcessarCreditoResponse>> ExecutarCreditoAsync(
            Conta conta,
            ProcessarCreditoCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(10, cancellationToken); // Simula processamento

                // Executar crédito na conta
                conta.Creditar(request.Valor, request.Descricao);

                Logger.Trace($"Crédito executado na conta {conta.Numero}: {request.Valor:C}");

                return Result<ProcessarCreditoResponse>.Success(null!); // Sucesso na execução
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn($"Operação de crédito inválida: {ex.Message}");
                return Result<ProcessarCreditoResponse>.Failure(ex.Message);
            }
            catch (ArgumentException ex)
            {
                Logger.Warn($"Argumentos inválidos para crédito: {ex.Message}");
                return Result<ProcessarCreditoResponse>.Failure(ex.Message);
            }
        }

        private async Task<bool> ValidarHorarioOperacaoAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);

            var agora = DateTime.Now;
            var isHorarioComercial = agora.Hour >= 6 && agora.Hour <= 22 && // 6h às 22h
                                    agora.DayOfWeek != DayOfWeek.Sunday; // Não aos domingos

            return !isHorarioComercial; // Retorna true se FORA do horário (para falhar a validação)
        }

        protected override Task OnHandling(ProcessarCreditoCommand request)
        {
            Logger.Info($"Iniciando handler de crédito - RequestId: {request.RequestId}");
            return Task.CompletedTask;
        }

        protected override Task OnHandled(ProcessarCreditoCommand request, Result<ProcessarCreditoResponse> result)
        {
            if (result.IsSuccess)
            {
                Logger.Info($"Handler de crédito concluído com sucesso - RequestId: {request.RequestId}");
            }
            return Task.CompletedTask;
        }

        protected override Task OnFailed(ProcessarCreditoCommand request, Result<ProcessarCreditoResponse> result)
        {
            Logger.Warn($"Handler de crédito falhou - RequestId: {request.RequestId}, Erro: {result.Error}");
            return Task.CompletedTask;
        }

        protected override Task OnException(ProcessarCreditoCommand request, Exception exception)
        {
            Logger.Error(exception, $"Exceção no handler de crédito - RequestId: {request.RequestId}");
            return Task.CompletedTask;
        }
    }

}
