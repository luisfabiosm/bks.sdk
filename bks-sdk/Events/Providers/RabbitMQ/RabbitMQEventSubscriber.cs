using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Observability.Logging;
using RabbitMQ.Client;

namespace bks.sdk.Events.Providers.RabbitMQ;

public class RabbitMQEventSubscriber : IEventSubscriber, IAsyncDisposable
{
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _exchangeName;
    private readonly Dictionary<string, string> _consumerTags = new();

    public RabbitMQEventSubscriber(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        _settings = settings;
        _logger = logger;
        _exchangeName = _settings.Events.AdditionalSettings.GetValueOrDefault("ExchangeName", "bks-framework-events");
    }

    public static async Task<RabbitMQEventSubscriber> CreateAsync(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        var subscriber = new RabbitMQEventSubscriber(settings, logger);
        await subscriber.InitializeAsync();
        return subscriber;
    }

    private async Task InitializeAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_settings.Events.ConnectionString)
                // DispatchConsumersAsync removido - não é mais necessário na v7+
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            _logger.Info("RabbitMQ subscriber conectado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao conectar subscriber RabbitMQ: {ex.Message}");
            throw;
        }
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel não inicializado. Use CreateAsync() para criar a instância.");

        var topic = topicOverride ?? GenerateTopicName<TEvent>();
        var queueName = $"{topic}-{Environment.MachineName}-{Guid.NewGuid():N}";

        // Declarar fila
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: true);

        // Bind fila ao exchange
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: topic);

        // Criar consumer
        var consumer = new RabbitMQAsyncEventConsumer<TEvent>(_channel, handler, _logger);
        var consumerTag = await _channel.BasicConsumeAsync(queueName, false, consumer);

        _consumerTags[typeof(TEvent).FullName!] = consumerTag;

        _logger.Info($"Inscrito no tópico: {topic} - Fila: {queueName}");
    }

    public async Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        await SubscribeAsync<TEvent>(handler.HandleAsync, topicOverride);
    }

    public async Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent
    {
        if (_channel == null) return;

        var eventTypeName = typeof(TEvent).FullName!;
        if (_consumerTags.TryGetValue(eventTypeName, out var consumerTag))
        {
            await _channel.BasicCancelAsync(consumerTag);
            _consumerTags.Remove(eventTypeName);

            _logger.Info($"Desinscrito do evento: {eventTypeName}");
        }
    }

    private string GenerateTopicName<TEvent>() where TEvent : IDomainEvent
    {
        var instance = Activator.CreateInstance<TEvent>();
        var prefix = _settings.Events.TopicPrefix;
        var eventType = instance.EventType.Replace('.', '-');
        return $"{prefix}-{eventType}".ToLowerInvariant();
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            foreach (var consumerTag in _consumerTags.Values)
            {
                await _channel.BasicCancelAsync(consumerTag);
            }

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