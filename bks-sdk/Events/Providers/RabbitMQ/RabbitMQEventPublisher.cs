using bks.sdk.Core.Configuration;
using bks.sdk.Events.Publishers;
using bks.sdk.Observability.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.RabbitMQ;

public class RabbitMQEventPublisher : EventPublisherBase, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;

    public RabbitMQEventPublisher(BKSFrameworkSettings settings, IBKSLogger logger)
        : base(settings, logger)
    {
        try
        {
            _exchangeName = Settings.Events.AdditionalSettings.GetValueOrDefault("ExchangeName", "bks-framework-events");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(Settings.Events.ConnectionString),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            Logger.Info($"RabbitMQ conectado com sucesso - Exchange: {_exchangeName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao conectar com RabbitMQ: {ex.Message}");
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
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = domainEvent.EventId;
            properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)domainEvent.OccurredOn).ToUnixTimeSeconds());
            properties.Type = domainEvent.EventType;

            if (!string.IsNullOrWhiteSpace(domainEvent.CorrelationId))
            {
                properties.CorrelationId = domainEvent.CorrelationId;
            }

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: topic,
                basicProperties: properties,
                body: body);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao publicar no RabbitMQ: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
