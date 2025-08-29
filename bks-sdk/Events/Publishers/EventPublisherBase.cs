using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Observability.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bks.sdk.Events.Publishers;

    public abstract class EventPublisherBase : IEventPublisher
{
    protected readonly BKSFrameworkSettings Settings;
    protected readonly IBKSLogger Logger;

    protected EventPublisherBase(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        Settings = settings;
        Logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        await PublishAsync(domainEvent, null, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var tasks = domainEvents.Select(evt => PublishAsync(evt, null, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, string? topicOverride, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        try
        {
            Logger.Trace($"Publicando evento: {domainEvent.EventType} - EventId: {domainEvent.EventId}");

            var topic = topicOverride ?? GenerateTopicName(domainEvent);
            var message = SerializeEvent(domainEvent);

            await PublishToProviderAsync(topic, message, domainEvent, cancellationToken);

            Logger.Trace($"Evento publicado com sucesso: {domainEvent.EventId} - Tópico: {topic}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao publicar evento: {domainEvent.EventId} - Erro: {ex.Message}");
            throw;
        }
    }

    protected abstract Task PublishToProviderAsync(
        string topic,
        string message,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken);

    protected virtual string GenerateTopicName(IDomainEvent domainEvent)
    {
        var prefix = Settings.Events.TopicPrefix;
        var eventType = domainEvent.EventType.Replace('.', '-');
        return $"{prefix}-{eventType}".ToLowerInvariant();
    }

    protected virtual string SerializeEvent(IDomainEvent domainEvent)
    {
        var eventData = new
        {
            EventId = domainEvent.EventId,
            EventType = domainEvent.EventType,
            OccurredOn = domainEvent.OccurredOn,
            CorrelationId = domainEvent.CorrelationId,
            Metadata = domainEvent.Metadata,
            Data = domainEvent
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(eventData, options);
    }
}
