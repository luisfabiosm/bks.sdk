namespace bks.sdk.Events;

public interface IEventBroker
{
    Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent;
}