using bks.sdk.Observability.Logging;
using Domain.Core.Entities;
using Domain.Core.Ports.Domain;
using System.Collections.Concurrent;

namespace Domain.Services
{
    public class AuditService : IAuditService
    {
        private readonly IBKSLogger _logger;
        private static readonly ConcurrentDictionary<string, AuditEntry> _auditLog = new();

        public AuditService(IBKSLogger logger)
        {
            _logger = logger;
        }

        public async Task LogTransactionStartAsync(
            string transactionId,
            string transactionType,
            DateTime startedAt,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken); // Simula persistência

            var auditEntry = new AuditEntry
            {
                TransactionId = transactionId,
                TransactionType = transactionType,
                Status = "STARTED",
                Timestamp = startedAt,
                Details = new Dictionary<string, object>
                {
                    ["started_at"] = startedAt,
                    ["source"] = "API"
                }
            };

            _auditLog.TryAdd(transactionId, auditEntry);

            _logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Information,
                "Audit: Transaction started",
                auditEntry);

            _logger.Info($"🔍 AUDIT: Transação iniciada - {transactionId} ({transactionType})");
        }

        public async Task LogTransactionCompletedAsync(
            string transactionId,
            string transactionType,
            DateTime completedAt,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            if (_auditLog.TryGetValue(transactionId, out var existingEntry))
            {
                existingEntry.Status = "COMPLETED";
                existingEntry.CompletedAt = completedAt;
                existingEntry.Duration = duration;
                existingEntry.Details["completed_at"] = completedAt;
                existingEntry.Details["duration_ms"] = duration.TotalMilliseconds;
                existingEntry.Details["success"] = true;

                _logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Information,
                    "Audit: Transaction completed",
                    existingEntry);
            }

            _logger.Info($"✅ AUDIT: Transação completada - {transactionId} em {duration.TotalMilliseconds:F2}ms");
        }

        public async Task LogTransactionFailedAsync(
            string transactionId,
            string transactionType,
            string error,
            string? stackTrace,
            DateTime failedAt,
            CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);

            if (_auditLog.TryGetValue(transactionId, out var existingEntry))
            {
                existingEntry.Status = "FAILED";
                existingEntry.CompletedAt = failedAt;
                existingEntry.Error = error;
                existingEntry.Details["failed_at"] = failedAt;
                existingEntry.Details["error"] = error;
                existingEntry.Details["stack_trace"] = stackTrace;
                existingEntry.Details["success"] = false;

                _logger.LogStructured(bks.sdk.Common.Enums.LogLevel.Error,
                    "Audit: Transaction failed",
                    existingEntry);
            }

            _logger.Error($"❌ AUDIT: Transação falhada - {transactionId}: {error}");
        }

        public async Task<IEnumerable<AuditEntry>> GetAuditHistoryAsync(
            string? transactionId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            var entries = _auditLog.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                entries = entries.Where(e => e.TransactionId == transactionId);
            }

            if (startDate.HasValue)
            {
                entries = entries.Where(e => e.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                entries = entries.Where(e => e.Timestamp <= endDate.Value);
            }

            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }

        public static void ClearAuditLog()
        {
            _auditLog.Clear();
        }
    }

}
