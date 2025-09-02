
namespace Domain.Core.Ports.Domain
{
    public interface IAuditService
    {
        Task LogTransactionStartAsync(string transactionId, string transactionType, DateTime startedAt, CancellationToken cancellationToken);
        Task LogTransactionCompletedAsync(string transactionId, string transactionType, DateTime completedAt, TimeSpan duration, CancellationToken cancellationToken);
        Task LogTransactionFailedAsync(string transactionId, string transactionType, string error, string? stackTrace, DateTime failedAt, CancellationToken cancellationToken);
        //Task<IEnumerable<object>> GetAuditHistoryAsync(object value, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    }

}
