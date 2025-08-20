using Confluent.Kafka;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Events.Implementations;

public class KafkaEventBroker : IEventBroker, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ProducerConfig _producerConfig;
    private readonly string _topicPrefix;
    private readonly Observability.Logging.IBKSLogger _logger;

    public KafkaEventBroker(string connectionString, Observability.Logging.IBKSLogger logger)
    {
        _logger = logger;
        _topicPrefix = "bks-sdk-events";

        _producerConfig = new ProducerConfig
        {
            BootstrapServers = connectionString,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(_producerConfig).Build();
        _logger.Info($"Kafka Event Broker conectado: {connectionString}");
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        try
        {
            var topic = $"{_topicPrefix}-{domainEvent.EventType.Replace(".", "-")}";
            var message = JsonSerializer.Serialize(domainEvent);

            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(domainEvent.EventType) },
                    { "timestamp", Encoding.UTF8.GetBytes(domainEvent.OccurredOn.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            _logger.Info($"Evento {domainEvent.EventType} publicado no Kafka: {result.TopicPartitionOffset}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao publicar evento no Kafka: {ex.Message}");
            throw;
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IDomainEvent
    {
        try
        {
            var eventType = typeof(TEvent).Name.Replace("Event", "").ToLowerInvariant();
            var topic = $"{_topicPrefix}-{eventType}";
            var groupId = $"bks-sdk-{eventType}-consumer";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _producerConfig.BootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _ = Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
                consumer.Subscribe(topic);

                _logger.Info($"Subscrito ao tópico {topic} no Kafka");

                while (true)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                        if (consumeResult != null)
                        {
                            var domainEvent = JsonSerializer.Deserialize<TEvent>(consumeResult.Message.Value);
                            if (domainEvent != null)
                            {
                                await handler(domainEvent);
                                consumer.Commit(consumeResult);
                                _logger.Info($"Evento processado: {consumeResult.TopicPartitionOffset}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Erro ao processar evento do Kafka: {ex.Message}");
                    }
                }
            });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao subscrever evento no Kafka: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}