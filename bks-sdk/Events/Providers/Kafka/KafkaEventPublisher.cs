using bks.sdk.Core.Configuration;
using bks.sdk.Events.Publishers;
using bks.sdk.Observability.Logging;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.Kafka;


public class KafkaEventPublisher : EventPublisherBase, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(BKSFrameworkSettings settings, IBKSLogger logger)
        : base(settings, logger)
    {
        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = Settings.Events.ConnectionString,
                Acks = Acks.All,
                EnableIdempotence = true
            };

            // Aplicar configurações adicionais se fornecidas
            foreach (var kvp in Settings.Events.AdditionalSettings)
            {
                config.Set(kvp.Key, kvp.Value);
            }

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => Logger.Error($"Kafka Producer Error: {e.Reason}"))
                .SetLogHandler((_, message) => Logger.Trace($"Kafka Producer Log: {message.Message}"))
                .Build();

            Logger.Info("Kafka producer inicializado com sucesso");
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao inicializar Kafka producer: {ex.Message}");
            throw;
        }
    }

    protected override async Task PublishToProviderAsync(
        string topic,
        string message,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = domainEvent.EventId,
                Value = message,
                Headers = new Headers
                {
                    { "EventType", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventType) },
                    { "OccurredOn", System.Text.Encoding.UTF8.GetBytes(domainEvent.OccurredOn.ToString("O")) }
                }
            };

            if (!string.IsNullOrWhiteSpace(domainEvent.CorrelationId))
            {
                kafkaMessage.Headers.Add("CorrelationId", System.Text.Encoding.UTF8.GetBytes(domainEvent.CorrelationId));
            }

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            Logger.Trace($"Evento publicado no Kafka - Tópico: {topic}, Partição: {result.Partition}, Offset: {result.Offset}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao publicar no Kafka: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
