using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.InMemory;

public class InMemoryEventDispatcher
{
    private readonly List<InMemoryEventPublisher> _publishers = new();

    public void RegisterPublisher(InMemoryEventPublisher publisher)
    {
        _publishers.Add(publisher);
    }

    public async Task DispatchAsync(string topic, string message, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var tasks = _publishers.Select(p => p.ProcessEventAsync(topic, message, domainEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }
}

