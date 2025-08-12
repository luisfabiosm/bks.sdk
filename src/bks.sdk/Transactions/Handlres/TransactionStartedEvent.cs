using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public record TransactionStartedEvent : DomainEvent
    {
        public required string TransactionId { get; init; }
        public required string CorrelationId { get; init; }
        public required string TransactionType { get; init; }
        public required string ApplicationId { get; init; }
        public string? UserId { get; init; }
        public required DateTimeOffset StartedAt { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

}
