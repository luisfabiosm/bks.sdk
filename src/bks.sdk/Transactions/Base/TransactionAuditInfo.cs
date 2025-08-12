

using bks.sdk.Core.Enums;

namespace bks.sdk.Transactions.Base
{

    public record TransactionAuditInfo
    {

        public string AuditId { get; init; } = Guid.NewGuid().ToString("N");

        public required string TransactionId { get; init; }

        public required string CorrelationId { get; init; }

        public required string TransactionType { get; init; }

        public required TransactionStatus Status { get; init; }

        public required string ApplicationId { get; init; }

        public string? UserId { get; init; }

        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        public TimeSpan? Duration { get; init; }

        public string? InputDataHash { get; init; }

        public string? OutputDataHash { get; init; }

        public string? SecureToken { get; init; }

        public string? ErrorMessage { get; init; }

        public string? ErrorCode { get; init; }

        public string? IpAddress { get; init; }

        public string? UserAgent { get; init; }

        public Dictionary<string, object> Metadata { get; init; } = new();

        public static TransactionAuditInfo ForStart<T>(BaseTransaction<T> transaction, TransactionContext context)
        {
            return new TransactionAuditInfo
            {
                TransactionId = transaction.TransactionId,
                CorrelationId = transaction.CorrelationId,
                TransactionType = transaction.GetType().FullName!,
                Status = TransactionStatus.Processing,
                ApplicationId = context.ApplicationId,
                UserId = context.UserId,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                InputDataHash = transaction.ComputeHash(),
                Metadata = new Dictionary<string, object>(transaction.Metadata)
            };
        }

        public static TransactionAuditInfo ForCompletion<T>(
            BaseTransaction<T> transaction,
            TransactionContext context,
            TransactionResult<T> result,
            TimeSpan duration)
        {
            return new TransactionAuditInfo
            {
                TransactionId = transaction.TransactionId,
                CorrelationId = transaction.CorrelationId,
                TransactionType = transaction.GetType().FullName!,
                Status = result.Success ? TransactionStatus.Completed : TransactionStatus.Failed,
                ApplicationId = context.ApplicationId,
                UserId = context.UserId,
                Duration = duration,
                SecureToken = result.SecureToken,
                ErrorMessage = result.Success ? null : result.Message,
                ErrorCode = result.ErrorCode,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                Metadata = new Dictionary<string, object>(result.Metadata)
            };
        }
    }


}
