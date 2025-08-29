using bks.sdk.Events.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Extensions;


public class NoOpEventSubscriber : IEventSubscriber
{
    public Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }
}