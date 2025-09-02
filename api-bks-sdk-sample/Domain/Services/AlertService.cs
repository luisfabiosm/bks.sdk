using bks.sdk.Observability.Logging;
using Domain.Core.Entities;
using Domain.Core.Enums;
using Domain.Core.Ports.Domain;

namespace Domain.Services
{
    public class AlertService : IAlertService
    {
        private readonly IBKSLogger _logger;
        private static readonly List<AlertEntry> _alerts = new();

        public AlertService(IBKSLogger logger)
        {
            _logger = logger;
        }

        public async Task SendAlertAsync(
            string title,
            string message,
            AlertPriority priority,
            CancellationToken cancellationToken)
        {
            await Task.Delay(30, cancellationToken); // Simula envio de alerta

            var alert = new AlertEntry
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Priority = priority.ToString(),
                Timestamp = DateTime.UtcNow,
                Status = "SENT",
                Recipients = GetRecipientsForPriority(priority)
            };

            _alerts.Add(alert);

            var priorityIcon = priority switch
            {
                AlertPriority.Critical => "🔥",
                AlertPriority.High => "🚨",
                AlertPriority.Medium => "⚠️",
                AlertPriority.Low => "ℹ️",
                _ => "📢"
            };

            _logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Warning,
                $"Alert sent: {priority}",
                alert);

            _logger.Warn($"{priorityIcon} ALERTA [{priority}]: {title} - {message}");
        }

        public async Task SendCriticalAlertAsync(
            string title,
            string message,
            CancellationToken cancellationToken)
        {
            await SendAlertAsync(title, message, AlertPriority.Critical, cancellationToken);

            // Para alertas críticos, também logar como erro
            _logger.Error($"🔥 CRÍTICO: {title} - {message}");
        }

        public async Task SendSystemHealthAlertAsync(
            string component,
            string status,
            string details,
            CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken);

            var title = $"Sistema - {component}";
            var message = $"Status: {status}\nDetalhes: {details}";
            var priority = status.ToLowerInvariant() switch
            {
                "down" or "error" or "failed" => AlertPriority.Critical,
                "degraded" or "warning" => AlertPriority.High,
                "recovering" => AlertPriority.Medium,
                _ => AlertPriority.Low
            };

            await SendAlertAsync(title, message, priority, cancellationToken);
        }

        private List<string> GetRecipientsForPriority(AlertPriority priority)
        {
            return priority switch
            {
                AlertPriority.Critical => new List<string> { "admin@empresa.com", "supervisor@empresa.com", "operacoes@empresa.com" },
                AlertPriority.High => new List<string> { "supervisor@empresa.com", "operacoes@empresa.com" },
                AlertPriority.Medium => new List<string> { "operacoes@empresa.com" },
                AlertPriority.Low => new List<string> { "operacoes@empresa.com" },
                _ => new List<string> { "operacoes@empresa.com" }
            };
        }

        public async Task<IEnumerable<AlertEntry>> GetAlertHistoryAsync(
            AlertPriority? priority = null,
            DateTime? startDate = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            var alerts = _alerts.AsEnumerable();

            if (priority.HasValue)
            {
                alerts = alerts.Where(a => a.Priority == priority.Value.ToString());
            }

            if (startDate.HasValue)
            {
                alerts = alerts.Where(a => a.Timestamp >= startDate.Value);
            }

            return alerts.OrderByDescending(a => a.Timestamp).Take(100).ToList();
        }

        public static void ClearAlerts()
        {
            _alerts.Clear();
        }
    }

}
