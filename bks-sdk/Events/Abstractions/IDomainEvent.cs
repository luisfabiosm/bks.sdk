public interface IDomainEvent
{
    string EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    string? CorrelationId { get; }
    Dictionary<string, object> Metadata { get; }
}
