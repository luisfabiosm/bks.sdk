using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Providers.RabbitMQ.bks.sdk.Events.Providers.RabbitMQ;
using bks.sdk.Observability.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.RabbitMQ;

public class RabbitMQEventSubscriber : IEventSubscriber, IDisposable
{
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly Dictionary<string, string> _consumerTags = new();

    public RabbitMQEventSubscriber(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        _settings = settings;
        _logger = logger;

        try
        {
            _exchangeName = _settings.Events.AdditionalSettings.GetValueOrDefault("ExchangeName", "bks-framework-events");

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_settings.Events.ConnectionString),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

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
        var topic = topicOverride ?? GenerateTopicName<TEvent>();
        var queueName = $"{topic}-{Environment.MachineName}-{Guid.NewGuid():N}";

        // Declarar fila
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: true);

        // Bind fila ao exchange
        _channel.QueueBind(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: topic);

        // Criar consumer
        var consumer = new RabbitMQAsyncEventConsumer<TEvent>(_channel, handler, _logger);
        var consumerTag = _channel.BasicConsume(queueName, false, consumer);

        _consumerTags[typeof(TEvent).FullName!] = consumerTag;

        _logger.Info($"Inscrito no tópico: {topic} - Fila: {queueName}");

        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        await SubscribeAsync<TEvent>(handler.HandleAsync, topicOverride);
    }

    public async Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent
    {
        var eventTypeName = typeof(TEvent).FullName!;
        if (_consumerTags.TryGetValue(eventTypeName, out var consumerTag))
        {
            _channel.BasicCancel(consumerTag);
            _consumerTags.Remove(eventTypeName);

            _logger.Info($"Desinscrito do evento: {eventTypeName}");
        }

        await Task.CompletedTask;
    }

    private string GenerateTopicName<TEvent>() where TEvent : IDomainEvent
    {
        var instance = Activator.CreateInstance<TEvent>();
        var prefix = _settings.Events.TopicPrefix;
        var eventType = instance.EventType.Replace('.', '-');
        return $"{prefix}-{eventType}".ToLowerInvariant();
    }

    public void Dispose()
    {
        foreach (var consumerTag in _consumerTags.Values)
        {
            _channel?.BasicCancel(consumerTag);
        }

        _channel?.Dispose();
        _connection?.Dispose();
    }
}

