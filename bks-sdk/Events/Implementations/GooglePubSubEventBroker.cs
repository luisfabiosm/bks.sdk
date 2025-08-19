using Google.Cloud.PubSub.V1;
using System.Text.Json;

namespace bks.sdk.Events.Implementations;

public class GooglePubSubEventBroker : IEventBroker, IDisposable
{
    private readonly PublisherClient _publisher;
    private readonly string _projectId;
    private readonly string _topicPrefix;
    private readonly Observability.Logging.ILogger _logger;

    public GooglePubSubEventBroker(string connectionString, Observability.Logging.ILogger logger)
    {
        _logger = logger;

        _projectId = connectionString.Split('/').Last();
        _topicPrefix = "bks-sdk-events";

        var defaultTopicName = TopicName.FromProjectTopic(_projectId, _topicPrefix);
        _publisher = PublisherClient.Create(defaultTopicName);
        _logger.Info($"Google Pub/Sub Event Broker conectado: {connectionString}");
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        try
        {
            var topicName = $"{_topicPrefix}-{domainEvent.EventType.Replace(".", "-")}";
            var topicId = TopicName.FromProjectTopic(_projectId, topicName);

            var message = JsonSerializer.Serialize(domainEvent);
            var pubsubMessage = new PubsubMessage
            {
                Data = Google.Protobuf.ByteString.CopyFromUtf8(message),
                Attributes =
                {
                    { "event-type", domainEvent.EventType },
                    { "timestamp", domainEvent.OccurredOn.ToString("O") },
                    { "message-id", Guid.NewGuid().ToString() }
                }
            };

            var messageId = await _publisher.PublishAsync(topicId.ToString(), message);
            _logger.Info($"Evento {domainEvent.EventType} publicado no Pub/Sub: {messageId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao publicar evento no Pub/Sub: {ex.Message}");
            throw;
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        try
        {
            var eventType = typeof(TEvent).Name.Replace("Event", "").ToLowerInvariant();
            var subscriptionName = $"{_topicPrefix}-{eventType}-subscription";
            var subscriptionId = SubscriptionName.FromProjectSubscription(_projectId, subscriptionName);

            var subscriber = SubscriberClient.Create(subscriptionId);

            await subscriber.StartAsync(async (message, cancellationToken) =>
            {
                try
                {
                    var messageText = message.Data.ToStringUtf8();
                    var domainEvent = JsonSerializer.Deserialize<TEvent>(messageText);

                    if (domainEvent != null)
                    {
                        await handler(domainEvent);
                        _logger.Info($"Evento {message.MessageId} processado com sucesso");
                        return SubscriberClient.Reply.Ack;
                    }

                    return SubscriberClient.Reply.Nack;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao processar evento do Pub/Sub: {ex.Message}");
                    return SubscriberClient.Reply.Nack;
                }
            });

            _logger.Info($"Subscrito aos eventos {typeof(TEvent).Name} no Pub/Sub");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao subscrever evento no Pub/Sub: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        // Pub/Sub clients são thread-safe e podem ser reutilizados
        // Normalmente não precisam ser disposed explicitamente
    }
}