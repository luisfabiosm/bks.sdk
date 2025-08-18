namespace bks.sdk.Events;

public class RabbitMqEventBroker : IEventBroker
{
    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // TODO: Implement RabbitMQ publish logic
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        // TODO: Implement RabbitMQ subscribe logic
        return Task.CompletedTask;
    }
}