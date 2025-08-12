using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public record TransactionErrorEvent : DomainEvent
    {
        public required string TransactionId { get; init; }
        public required string CorrelationId { get; init; }
        public required string TransactionType { get; init; }
        public required string ApplicationId { get; init; }
        public string? UserId { get; init; }
        public required DateTimeOffset ErroredAt { get; init; }
        public required string ErrorType { get; init; }
        public required string ErrorMessage { get; init; }
        public string? ErrorDetail { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

