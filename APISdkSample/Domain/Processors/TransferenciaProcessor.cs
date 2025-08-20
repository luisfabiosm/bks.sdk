using bks.sdk.Common.Results;
using bks.sdk.Enum;
using bks.sdk.Events;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Transactions;
using Domain.Core.Enums;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Transactions;

namespace Domain.Processors
{
    public class TransferenciaProcessor : TransactionProcessor
    {
        private readonly IContaRepository _contaRepository;
        private readonly ITaxaService _taxaService;
        private readonly ILimiteService _limiteService;
        private readonly IAntiFraudeService _antiFraudeService;
        private readonly INotificationService _notificationService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IBacenService _bacenService;

        public TransferenciaProcessor(IServiceProvider  serviceProvider,
                                 IBKSLogger logger,
                                 IBKSTracer tracer,
                                 IEventBroker eventBroker) : base(serviceProvider,logger, tracer, eventBroker)

        {
            _contaRepository = serviceProvider.GetRequiredService<IContaRepository>();
            _taxaService = serviceProvider.GetRequiredService<ITaxaService>(); 
            _limiteService = serviceProvider.GetRequiredService<ILimiteService>();
            _antiFraudeService = serviceProvider.GetRequiredService<IAntiFraudeService>(); 
            _notificationService = serviceProvider.GetRequiredService<INotificationService>(); 
            _auditoriaService = serviceProvider.GetRequiredService<IAuditoriaService>(); 
            _bacenService = serviceProvider.GetRequiredService<IBacenService>(); 

        }


        // PRÉ-PROCESSAMENTO: Validações, análises e preparação
        protected override async Task<Result> PreProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            var transferencia = (TransferenciaTransaction)transaction;

            // 1. Validações de negócio específicas
            if (transferencia.Valor <= 0)
                return Result.Failure("Valor deve ser maior que zero");

            if (transferencia.NumeroContaOrigem == transferencia.NumeroContaDestino)
                return Result.Failure("Conta origem e destino devem ser diferentes");

            // 2. Verificar existência e status das contas
            var contaOrigem = await _contaRepository.GetByNumeroAsync(transferencia.NumeroContaOrigem, cancellationToken);
            var contaDestino = await _contaRepository.GetByNumeroAsync(transferencia.NumeroContaDestino, cancellationToken);

            if (contaOrigem == null)
                return Result.Failure("Conta de origem não encontrada");

            if (contaDestino == null)
                return Result.Failure("Conta de destino não encontrada");

            if (!contaOrigem.Ativa || !contaDestino.Ativa)
                return Result.Failure("Uma das contas está inativa");

            // 3. Verificar limites do cliente
            var limiteResult = await _limiteService.VerificarLimitesAsync(
                transferencia.NumeroContaOrigem,
                transferencia.Valor,
                transferencia.Tipo,
                cancellationToken);

            if (!limiteResult.Aprovado)
                return Result.Failure($"Limite excedido: {limiteResult.Motivo}");

            // 4. Calcular taxas
            var taxa = await _taxaService.CalcularTaxaAsync(
                transferencia.Tipo,
                transferencia.Valor,
                cancellationToken);

            transferencia.SetTaxa(taxa);

            // 5. Verificar saldo suficiente (valor + taxa)
            var valorTotal = transferencia.Valor + taxa;
            if (contaOrigem.Saldo < valorTotal)
                return Result.Failure("Saldo insuficiente para a operação (incluindo taxa)");

            // 6. Análise antifraude para valores altos
            if (transferencia.Valor > 10000)
            {
                var analiseResult = await _antiFraudeService.AnalisarTransferenciaAsync(transferencia, cancellationToken);
                if (!analiseResult.Aprovado)
                    return Result.Failure($"Operação bloqueada por segurança: {analiseResult.Motivo}");
            }

            // 7. Reservar saldo (pré-autorização)
            var reservaResult = await _contaRepository.ReservarSaldoAsync(
                transferencia.NumeroContaOrigem,
                valorTotal,
                transferencia.CorrelationId,
                cancellationToken);

            if (!reservaResult.Success)
                return Result.Failure("Falha ao reservar saldo");

            // 8. Registrar início da operação para auditoria
            await _auditoriaService.RegistrarInicioOperacaoAsync(
                transferencia.CorrelationId,
                "TRANSFERENCIA",
                transferencia.Valor,
                transferencia.NumeroContaOrigem,
                transferencia.NumeroContaDestino,
                cancellationToken);

            return Result.Success();
        }

        protected override Task<Result> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        


    }
}
