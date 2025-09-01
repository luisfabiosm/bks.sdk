public abstract record DomainEvent : IDomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public string? CorrelationId { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
