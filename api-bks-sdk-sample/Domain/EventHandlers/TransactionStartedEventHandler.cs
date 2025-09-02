using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using Domain.Core.Ports.Domain;
using Domain.Core.Ports.Outbound;

namespace Domain.EventHandlers
{
    public class TransactionStartedEventHandler : BaseEventHandler<TransactionStartedEvent>
    {
        private readonly IContaRepository _contaRepository;
        private readonly IAuditService _auditService;

        public TransactionStartedEventHandler(
            IBKSLogger logger,
            IBKSTracer tracer,
            IContaRepository contaRepository,
            IAuditService auditService) : base(logger, tracer)
        {
            _contaRepository = contaRepository;
            _auditService = auditService;
        }

        protected override async Task ProcessEventAsync(
            TransactionStartedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            Logger.Info($"Processando evento de transação iniciada: {domainEvent.TransactionId}");

            // 1. Registrar início da transação para auditoria
            await _auditService.LogTransactionStartAsync(
                domainEvent.TransactionId,
                domainEvent.TransactionType,
                domainEvent.StartedAt,
                cancellationToken);

            // 2. Verificar se precisa de notificações especiais
            await ProcessNotificationsIfNeeded(domainEvent, cancellationToken);

            // 3. Atualizar métricas internas
            await UpdateInternalMetrics(domainEvent, cancellationToken);

            // 4. Preparar dados para monitoramento
            await PrepareMonitoringData(domainEvent, cancellationToken);

            Logger.Info($"Evento de transação iniciada processado: {domainEvent.TransactionId}");
        }

        private async Task ProcessNotificationsIfNeeded(
            TransactionStartedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            // Para transações de alto valor, preparar notificação
            if (domainEvent.Metadata.ContainsKey("Valor"))
            {
                if (decimal.TryParse(domainEvent.Metadata["Valor"].ToString(), out var valor))
                {
                    if (valor > 50000)
                    {
                        Logger.Info($"Transação de alto valor iniciada: {valor:C} - {domainEvent.TransactionId}");

                        // Aqui poderia enviar para um serviço de notificação
                        await NotifyHighValueTransaction(domainEvent, valor, cancellationToken);
                    }
                }
            }
        }

        private async Task UpdateInternalMetrics(
            TransactionStartedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken); // Simula atualização de métricas

            // Incrementar contadores por tipo de transação
            var metricsKey = $"transaction.started.{domainEvent.TransactionType.ToLowerInvariant()}";

            // Em uma implementação real, usaria um serviço de métricas como Prometheus
            Logger.Info($"Métrica incrementada: {metricsKey}");
        }

        private async Task PrepareMonitoringData(
            TransactionStartedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            // Preparar dados estruturados para o sistema de monitoramento
            var monitoringData = new
            {
                TransactionId = domainEvent.TransactionId,
                TransactionType = domainEvent.TransactionType,
                StartedAt = domainEvent.StartedAt,
                CorrelationId = domainEvent.CorrelationId,
                EventId = domainEvent.EventId,
                Metadata = domainEvent.Metadata
            };

            Logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Information,
                "Transaction monitoring data prepared",
                monitoringData,
                domainEvent.CorrelationId);
        }

        private async Task NotifyHighValueTransaction(
            TransactionStartedEvent domainEvent,
            decimal valor,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            // Simular envio de notificação para supervisores
            Logger.Info($"🚨 ALERTA: Transação de alto valor iniciada - " +
                       $"ID: {domainEvent.TransactionId}, Valor: {valor:C}, Tipo: {domainEvent.TransactionType}");
        }
    }

}
