using bks.sdk.Events.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Extensions;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<TEvent>(TEvent domainEvent, string? topicOverride, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return Task.CompletedTask;
    }
}
