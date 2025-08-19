using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Events.Implementations;

public class RabbitMqEventBroker : IEventBroker, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _publishChannel;
    private readonly string _exchangeName;
    private readonly Observability.Logging.ILogger _logger;
    private readonly ConcurrentDictionary<string, IChannel> _subscriptionChannels;
    private readonly SemaphoreSlim _publishSemaphore;
    private bool _disposed;

    public RabbitMqEventBroker(string connectionString, Observability.Logging.ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        _subscriptionChannels = new ConcurrentDictionary<string, IChannel>();
        _publishSemaphore = new SemaphoreSlim(1, 1);

        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60),
                ClientProvidedName = "bks-sdk-event-broker"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _publishChannel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _exchangeName = "bks-sdk-events";

            _publishChannel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null).GetAwaiter().GetResult();

            _logger.Info($"RabbitMQ Event Broker conectado: {connectionString}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Falha ao inicializar RabbitMQ Event Broker: {ex.Message}");
            throw new EventBrokerException("Falha ao inicializar conexão com RabbitMQ", ex);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        ThrowIfDisposed();

        await _publishSemaphore.WaitAsync();
        try
        {
            var message = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type = domainEvent.EventType,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent
            };

            var routingKey = GetRoutingKey(domainEvent.EventType);

            await _publishChannel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.Info($"Evento {domainEvent.EventType} publicado com ID {properties.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao publicar evento {domainEvent.EventType}: {ex.Message}");
            throw new EventBrokerException($"Falha ao publicar evento {domainEvent.EventType}", ex);
        }
        finally
        {
            _publishSemaphore.Release();
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        ThrowIfDisposed();

        var eventType = typeof(TEvent).Name.Replace("Event", "").ToLowerInvariant();
        var channelKey = $"subscription-{eventType}";

        try
        {
            // Create dedicated channel for this subscription
            var subscriptionChannel = await _connection.CreateChannelAsync();
            _subscriptionChannels.TryAdd(channelKey, subscriptionChannel);

            var queueName = $"bks-sdk-{eventType}-queue";
            var routingKey = $"*.{eventType}.*";

            // Declare queue with additional arguments for better reliability
            var queueArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", $"{_exchangeName}-dlx"},
                {"x-dead-letter-routing-key", $"failed.{eventType}"},
                {"x-message-ttl", 3600000}, // 1 hour TTL
                {"x-max-retries", 3}
            };

            await subscriptionChannel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            await subscriptionChannel.QueueBindAsync(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: routingKey,
                arguments: null);

            // Declare dead letter exchange and queue
            var dlxName = $"{_exchangeName}-dlx";
            var dlqName = $"bks-sdk-{eventType}-dlq";

            await subscriptionChannel.ExchangeDeclareAsync(
                exchange: dlxName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null);

            await subscriptionChannel.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await subscriptionChannel.QueueBindAsync(
                queue: dlqName,
                exchange: dlxName,
                routingKey: $"failed.{eventType}",
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(subscriptionChannel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var messageId = ea.BasicProperties?.MessageId ?? "unknown";

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var domainEvent = JsonSerializer.Deserialize<TEvent>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (domainEvent != null)
                    {
                        await handler(domainEvent);
                        await subscriptionChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        _logger.Info($"Evento {ea.RoutingKey} processado [MessageId: {messageId}]");
                    }
                    else
                    {
                        _logger.Error($"Falha na desserialização do evento [MessageId: {messageId}]");
                        await subscriptionChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.Error($"Erro de JSON ao processar evento [MessageId: {messageId}]: {jsonEx.Message}");
                    await subscriptionChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao processar evento [MessageId: {messageId}]: {ex.Message}");

                    // Simple retry logic - reject message and let RabbitMQ handle retry via DLQ
                    await subscriptionChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Configure QoS for better performance
            await subscriptionChannel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 10,
                global: false);

            var consumerTag = await subscriptionChannel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.Info($"Subscrito aos eventos {typeof(TEvent).Name} no RabbitMQ [ConsumerTag: {consumerTag}]");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao subscrever evento {typeof(TEvent).Name}: {ex.Message}");

            // Clean up the channel if subscription failed
            if (_subscriptionChannels.TryRemove(channelKey, out var failedChannel))
            {
                try
                {
                    failedChannel?.Dispose();
                }
                catch { /* Ignore cleanup errors */ }
            }

            throw new EventBrokerException($"Falha ao subscrever evento {typeof(TEvent).Name}", ex);
        }
    }

    private string GetRoutingKey(string eventType) =>
        $"event.{eventType.Replace(".", "-").ToLowerInvariant()}.v1";

    private int GetRedeliveryCount(object properties)
    {
        try
        {
            // Use reflection to access Headers property regardless of exact type
            var headersProperty = properties?.GetType().GetProperty("Headers");
            if (headersProperty?.GetValue(properties) is IDictionary<string, object> headers &&
                headers.TryGetValue("x-death", out var deathHeader) &&
                deathHeader is List<object> deaths && deaths.Count > 0)
            {
                if (deaths[0] is Dictionary<string, object> death &&
                    death.TryGetValue("count", out var countObj))
                {
                    return Convert.ToInt32(countObj);
                }
            }
        }
        catch
        {
            // If we can't get redelivery count, assume it's the first attempt
        }
        return 0;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqEventBroker));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                // Dispose all subscription channels
                foreach (var channel in _subscriptionChannels.Values)
                {
                    try
                    {
                        channel?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Erro ao fechar canal de subscription: {ex.Message}");
                    }
                }
                _subscriptionChannels.Clear();

                // Dispose publish channel
                _publishChannel?.Dispose();

                // Dispose connection
                _connection?.Dispose();

                // Dispose semaphore
                _publishSemaphore?.Dispose();

                _logger.Info("RabbitMQ Event Broker disposed com sucesso");
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao disposal do RabbitMqEventBroker: {ex.Message}");
            }
        }

        _disposed = true;
    }
}

public class EventBrokerException : Exception
{
    public EventBrokerException(string message) : base(message) { }

    public EventBrokerException(string message, Exception innerException)
        : base(message, innerException) { }
}