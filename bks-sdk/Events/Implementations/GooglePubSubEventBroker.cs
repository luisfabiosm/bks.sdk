namespace bks.sdk.Events.Implementations;

public class GooglePubSubEventBroker : IEventBroker
{
    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // Implementação real do Google Pub/Sub aqui
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        // Implementação real do Google Pub/Sub aqui
        return Task.CompletedTask;
    }
}