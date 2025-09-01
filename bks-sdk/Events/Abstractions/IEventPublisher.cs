using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Abstractions;
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    Task PublishAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    Task PublishAsync<TEvent>(TEvent domainEvent, string? topicOverride, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}


