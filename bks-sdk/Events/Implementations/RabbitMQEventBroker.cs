namespace bks.sdk.Events.Implementations;

public class RabbitMQEventBroker : IEventBroker
{
    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // Implementa��o real do RabbitMQ aqui
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        // Implementa��o real do RabbitMQ aqui
        return Task.CompletedTask;
    }
}