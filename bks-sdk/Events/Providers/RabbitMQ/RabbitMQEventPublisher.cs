using bks.sdk.Core.Configuration;
using bks.sdk.Events.Publishers;
using bks.sdk.Observability.Logging;
using RabbitMQ.Client;
using System.Text;

namespace bks.sdk.Events.Providers.RabbitMQ;

public class RabbitMQEventPublisher : EventPublisherBase, IAsyncDisposable
{
    private  IConnection _connection;
    private  IChannel _channel;
    private  string _exchangeName;

    public RabbitMQEventPublisher(BKSFrameworkSettings settings, IBKSLogger logger)
        : base(settings, logger)
    {
    }

    public static async Task<RabbitMQEventPublisher> CreateAsync(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        var publisher = new RabbitMQEventPublisher(settings, logger);
        await publisher.InitializeAsync();
        return publisher;
    }

    private async Task InitializeAsync()
    {
        try
        {
            _exchangeName = Settings.Events.AdditionalSettings.GetValueOrDefault("ExchangeName", "bks-framework-events");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(Settings.Events.ConnectionString)
                // DispatchConsumersAsync removido - não é mais necessário na v7+
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declarar exchange
            await _channel.ExchangeDeclareAsync(
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

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = domainEvent.EventId,
                Timestamp = new AmqpTimestamp(((DateTimeOffset)domainEvent.OccurredOn).ToUnixTimeSeconds()),
                Type = domainEvent.EventType
            };

            if (!string.IsNullOrWhiteSpace(domainEvent.CorrelationId))
            {
                properties.CorrelationId = domainEvent.CorrelationId;
            }
      
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: topic,
                true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error($"Erro ao publicar no RabbitMQ: {ex.Message}");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }

    // Manter compatibilidade com IDisposable
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }
}