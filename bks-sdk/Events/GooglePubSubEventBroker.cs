namespace bks.sdk.Events;

public class GooglePubSubEventBroker : IEventBroker
{
    public Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        // TODO: Implement Google Pub/Sub publish logic
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        // TODO: Implement Google Pub/Sub subscribe logic
        return Task.CompletedTask;
    }
}