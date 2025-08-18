namespace bks.sdk.Events.Implementations;

public class KafkaEventBroker : IEventBroker
{
    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // Implementa��o real do Kafka aqui
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        // Implementa��o real do Kafka aqui
        return Task.CompletedTask;
    }
}