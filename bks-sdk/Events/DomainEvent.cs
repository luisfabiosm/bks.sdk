namespace bks.sdk.Events;

public abstract class DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}