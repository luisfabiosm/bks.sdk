using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Abstractions;


public interface IEventSubscriber
{
    Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? topicOverride = null)
        where TEvent : IDomainEvent;

    Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent;

    Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent;
}


