using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using Domain.Core.Ports.Domain;
using Domain.Core.Ports.Outbound;

namespace Domain.EventHandlers
{
    public class TransactionCompletedEventHandler : BaseEventHandler<TransactionCompletedEvent>
    {
        private readonly IContaRepository _contaRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;

        public TransactionCompletedEventHandler(
            IBKSLogger logger,
            IBKSTracer tracer,
            IContaRepository contaRepository,
            IAuditService auditService,
            INotificationService notificationService) : base(logger, tracer)
        {
            _contaRepository = contaRepository;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        protected override async Task ProcessEventAsync(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            Logger.Info($"Processando evento de transação completada: {domainEvent.TransactionId}");

            // 1. Finalizar auditoria
            await _auditService.LogTransactionCompletedAsync(
                domainEvent.TransactionId,
                domainEvent.TransactionType,
                domainEvent.CompletedAt,
                domainEvent.Duration,
                cancellationToken);

            // 2. Enviar notificações se necessário
            await ProcessCompletionNotifications(domainEvent, cancellationToken);

            // 3. Atualizar estatísticas de performance
            await UpdatePerformanceStatistics(domainEvent, cancellationToken);

            // 4. Executar pós-processamento se necessário
            await ExecutePostProcessing(domainEvent, cancellationToken);

            Logger.Info($"Evento de transação completada processado: {domainEvent.TransactionId}");
        }

        private async Task ProcessCompletionNotifications(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            // Buscar informações da conta se disponível nos metadados
            if (domainEvent.Metadata.ContainsKey("ContaNumero"))
            {
                var numeroConta = int.Parse(domainEvent.Metadata["ContaNumero"].ToString());
                var conta = await _contaRepository.GetByNumeroAsync(numeroConta!, cancellationToken);

                if (conta != null)
                {
                    // Notificar cliente sobre a transação completada
                    await _notificationService.NotifyTransactionCompletedAsync(
                        conta,
                        domainEvent.TransactionType,
                        domainEvent.CompletedAt,
                        cancellationToken);

                    Logger.Info($"Notificação de conclusão enviada para conta: {numeroConta}");
                }
            }
        }

        private async Task UpdatePerformanceStatistics(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            // Registrar métricas de performance
            var durationMs = domainEvent.Duration.TotalMilliseconds;

            Logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Information,
                "Transaction performance recorded",
                new
                {
                    TransactionType = domainEvent.TransactionType,
                    DurationMs = durationMs,
                    CompletedAt = domainEvent.CompletedAt,
                    IsSlowTransaction = durationMs > 5000 // > 5 segundos
                },
                domainEvent.CorrelationId);

            // Alerta para transações lentas
            if (durationMs > 10000) // > 10 segundos
            {
                Logger.Warn($"⚠️ Transação lenta detectada: {domainEvent.TransactionId} " +
                           $"levou {durationMs}ms para completar");
            }
        }

        private async Task ExecutePostProcessing(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            // Executar regras de pós-processamento baseadas no tipo de transação
            switch (domainEvent.TransactionType.ToLowerInvariant())
            {
                case "creditotransaction":
                    await ProcessCreditoPostActions(domainEvent, cancellationToken);
                    break;

                case "debitotransaction":
                    await ProcessDebitoPostActions(domainEvent, cancellationToken);
                    break;
            }
        }

        private async Task ProcessCreditoPostActions(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Trace($"Executando pós-processamento para crédito: {domainEvent.TransactionId}");

            // Ações específicas para créditos completados
            // Ex: Atualizar limites, verificar promoções, etc.
        }

        private async Task ProcessDebitoPostActions(
            TransactionCompletedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Trace($"Executando pós-processamento para débito: {domainEvent.TransactionId}");

            // Ações específicas para débitos completados
            // Ex: Verificar saldos baixos, aplicar taxas, etc.
        }
    }

}
