using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Publishers;
using bks.sdk.Observability.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.InMemory;


public class InMemoryEventPublisher : EventPublisherBase, IEventSubscriber
{
    private readonly ConcurrentDictionary<string, List<Func<string, IDomainEvent, CancellationToken, Task>>> _handlers;
    private readonly InMemoryEventDispatcher _dispatcher;

    public InMemoryEventPublisher(BKSFrameworkSettings settings, IBKSLogger logger, InMemoryEventDispatcher dispatcher)
        : base(settings, logger)
    {
        _handlers = new ConcurrentDictionary<string, List<Func<string, IDomainEvent, CancellationToken, Task>>>();
        _dispatcher = dispatcher;
    }

    protected override async Task PublishToProviderAsync(
        string topic,
        string message,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(topic, message, domainEvent, cancellationToken);
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        var topic = topicOverride ?? GenerateTopicName(Activator.CreateInstance<TEvent>());

        var wrappedHandler = new Func<string, IDomainEvent, CancellationToken, Task>(async (t, evt, ct) =>
        {
            if (evt is TEvent typedEvent)
            {
                await handler(typedEvent, ct);
            }
        });

        _handlers.AddOrUpdate(topic,
            new List<Func<string, IDomainEvent, CancellationToken, Task>> { wrappedHandler },
            (key, existing) => { existing.Add(wrappedHandler); return existing; });

        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        await SubscribeAsync<TEvent>(handler.HandleAsync, topicOverride);
    }

    public async Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent
    {
        var topic = GenerateTopicName(Activator.CreateInstance<TEvent>());
        _handlers.TryRemove(topic, out _);
        await Task.CompletedTask;
    }

    internal async Task ProcessEventAsync(string topic, string message, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(topic, out var handlers))
        {
            var tasks = handlers.Select(h => h(topic, domainEvent, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
