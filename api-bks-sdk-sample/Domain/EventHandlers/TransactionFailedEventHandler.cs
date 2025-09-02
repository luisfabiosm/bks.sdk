using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using Domain.Core.Enums;
using Domain.Core.Ports.Domain;

namespace Domain.EventHandlers
{
    public class TransactionFailedEventHandler : BaseEventHandler<TransactionFailedEvent>
    {
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IAlertService _alertService;

        public TransactionFailedEventHandler(
            IBKSLogger logger,
            IBKSTracer tracer,
            IAuditService auditService,
            INotificationService notificationService,
            IAlertService alertService) : base(logger, tracer)
        {
            _auditService = auditService;
            _notificationService = notificationService;
            _alertService = alertService;
        }

        protected override async Task ProcessEventAsync(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            Logger.Error($"Processando evento de transação falhada: {domainEvent.TransactionId} - {domainEvent.Error}");

            // 1. Registrar falha na auditoria
            await _auditService.LogTransactionFailedAsync(
                domainEvent.TransactionId,
                domainEvent.TransactionType,
                domainEvent.Error,
                domainEvent.StackTrace,
                domainEvent.FailedAt,
                cancellationToken);

            // 2. Classificar o tipo de erro
            var errorCategory = ClassifyError(domainEvent.Error);

            // 3. Processar baseado na categoria do erro
            await ProcessErrorByCategory(domainEvent, errorCategory, cancellationToken);

            // 4. Gerar alertas se necessário
            await GenerateAlertsIfNeeded(domainEvent, errorCategory, cancellationToken);

            // 5. Tentar recuperação automática se aplicável
            await AttemptAutoRecovery(domainEvent, errorCategory, cancellationToken);

            Logger.Info($"Evento de transação falhada processado: {domainEvent.TransactionId}");
        }

        private ErrorCategory ClassifyError(string error)
        {
            var errorLower = error.ToLowerInvariant();

            if (errorLower.Contains("saldo") || errorLower.Contains("insuficiente"))
                return ErrorCategory.InsufficientFunds;

            if (errorLower.Contains("conta") && (errorLower.Contains("não encontrada") || errorLower.Contains("inativa")))
                return ErrorCategory.InvalidAccount;

            if (errorLower.Contains("validação") || errorLower.Contains("inválido"))
                return ErrorCategory.ValidationError;

            if (errorLower.Contains("timeout") || errorLower.Contains("tempo"))
                return ErrorCategory.TimeoutError;

            if (errorLower.Contains("conexão") || errorLower.Contains("rede"))
                return ErrorCategory.NetworkError;

            return ErrorCategory.SystemError;
        }

        private async Task ProcessErrorByCategory(
            TransactionFailedEvent domainEvent,
            ErrorCategory category,
            CancellationToken cancellationToken)
        {
            Logger.Info($"Processando erro categoria {category} para transação {domainEvent.TransactionId}");

            switch (category)
            {
                case ErrorCategory.InsufficientFunds:
                    await ProcessInsufficientFundsError(domainEvent, cancellationToken);
                    break;

                case ErrorCategory.InvalidAccount:
                    await ProcessInvalidAccountError(domainEvent, cancellationToken);
                    break;

                case ErrorCategory.ValidationError:
                    await ProcessValidationError(domainEvent, cancellationToken);
                    break;

                case ErrorCategory.TimeoutError:
                    await ProcessTimeoutError(domainEvent, cancellationToken);
                    break;

                case ErrorCategory.NetworkError:
                    await ProcessNetworkError(domainEvent, cancellationToken);
                    break;

                case ErrorCategory.SystemError:
                    await ProcessSystemError(domainEvent, cancellationToken);
                    break;
            }
        }

        private async Task ProcessInsufficientFundsError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            // Notificar cliente sobre saldo insuficiente
            Logger.Info($"Erro de saldo insuficiente processado para: {domainEvent.TransactionId}");

            // Aqui poderia sugerir outras opções para o cliente
        }

        private async Task ProcessInvalidAccountError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            Logger.Warn($"Tentativa de transação em conta inválida: {domainEvent.TransactionId}");

            // Poderia gerar alerta de segurança se necessário
        }

        private async Task ProcessValidationError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Info($"Erro de validação processado: {domainEvent.TransactionId}");
        }

        private async Task ProcessTimeoutError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Warn($"Timeout detectado: {domainEvent.TransactionId}");

            // Marcar para possível retry automático
        }

        private async Task ProcessNetworkError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Warn($"Erro de rede detectado: {domainEvent.TransactionId}");
        }

        private async Task ProcessSystemError(
            TransactionFailedEvent domainEvent,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            Logger.Error($"Erro de sistema crítico: {domainEvent.TransactionId}");

            // Gerar alerta de alta prioridade
            await _alertService.SendCriticalAlertAsync(
                "Sistema - Erro Crítico",
                $"Transação {domainEvent.TransactionId} falhou com erro de sistema: {domainEvent.Error}",
                cancellationToken);
        }

        private async Task GenerateAlertsIfNeeded(
            TransactionFailedEvent domainEvent,
            ErrorCategory category,
            CancellationToken cancellationToken)
        {
            // Gerar alertas para categorias críticas
            if (category == ErrorCategory.SystemError || category == ErrorCategory.NetworkError)
            {
                await _alertService.SendAlertAsync(
                    $"Falha de Transação - {category}",
                    $"TransactionId: {domainEvent.TransactionId}\nErro: {domainEvent.Error}",
                    AlertPriority.High,
                    cancellationToken);
            }
        }

        private async Task AttemptAutoRecovery(
            TransactionFailedEvent domainEvent,
            ErrorCategory category,
            CancellationToken cancellationToken)
        {
            // Tentar recuperação automática para certos tipos de erro
            if (category == ErrorCategory.TimeoutError || category == ErrorCategory.NetworkError)
            {
                Logger.Info($"Tentativa de recuperação automática para: {domainEvent.TransactionId}");

                // Em uma implementação real, poderia reprocessar a transação
                await Task.Delay(100, cancellationToken);
            }
        }
    }

}
