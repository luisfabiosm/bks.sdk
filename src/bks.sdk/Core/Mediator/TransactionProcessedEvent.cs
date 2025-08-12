using BKS.SDK.Core.Events;

namespace bks.sdk.Core.Mediator
{
    public record TransactionProcessedEvent : DomainEvent
    {
        public required string TransactionId { get; init; }
        public required string CorrelationId { get; init; }
        public required string TransactionType { get; init; }
        public required DateTimeOffset ProcessedAt { get; init; }
        public required bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
