namespace bks.sdk.Events;

public class DomainEventDispatcher
{
    private readonly List<Func<IDomainEvent, Task>> _handlers = new();

    public void RegisterHandler(Func<IDomainEvent, Task> handler)
        => _handlers.Add(handler);

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        foreach (var handler in _handlers)
        {
            await handler(domainEvent);
        }
    }
}