using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Observability.Logging;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Providers.Kafka;


public class KafkaEventSubscriber : IEventSubscriber, IDisposable
{
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;
    private readonly Dictionary<string, IConsumer<string, string>> _consumers = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new();

    public KafkaEventSubscriber(BKSFrameworkSettings settings, IBKSLogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        var topic = topicOverride ?? GenerateTopicName<TEvent>();
        var groupId = $"{_settings.ApplicationName}-{typeof(TEvent).Name}";

        var config = new ConsumerConfig
        {
            GroupId = groupId,
            BootstrapServers = _settings.Events.ConnectionString,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        // Aplicar configurações adicionais
        foreach (var kvp in _settings.Events.AdditionalSettings)
        {
            if (kvp.Key.StartsWith("Consumer."))
            {
                config.Set(kvp.Key.Substring(9), kvp.Value);
            }
        }

        var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.Error($"Kafka Consumer Error: {e.Reason}"))
            .SetLogHandler((_, message) => _logger.Trace($"Kafka Consumer Log: {message.Message}"))
            .Build();

        consumer.Subscribe(topic);
        _consumers[topic] = consumer;

        var cts = new CancellationTokenSource();
        _cancellationTokens[topic] = cts;

        // Iniciar loop de consumo em background
        _ = Task.Run(async () => await ConsumeLoop(consumer, handler, cts.Token), cts.Token);

        _logger.Info($"Inscrito no tópico Kafka: {topic} - Grupo: {groupId}");

        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TEvent>(IEventHandler<TEvent> handler, string? topicOverride = null)
        where TEvent : IDomainEvent
    {
        await SubscribeAsync<TEvent>(handler.HandleAsync, topicOverride);
    }

    public async Task UnsubscribeAsync<TEvent>() where TEvent : IDomainEvent
    {
        var topic = GenerateTopicName<TEvent>();

        if (_cancellationTokens.TryGetValue(topic, out var cts))
        {
            cts.Cancel();
            _cancellationTokens.Remove(topic);
        }

        if (_consumers.TryGetValue(topic, out var consumer))
        {
            consumer.Close();
            consumer.Dispose();
            _consumers.Remove(topic);

            _logger.Info($"Desinscrito do tópico Kafka: {topic}");
        }

        await Task.CompletedTask;
    }

    private async Task ConsumeLoop<TEvent>(
        IConsumer<string, string> consumer,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(cancellationToken);

                try
                {
                    var eventData = System.Text.Json.JsonSerializer.Deserialize<EventWrapper<TEvent>>(
                        consumeResult.Message.Value,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        });

                    if (eventData.Data != null)
                    {
                        await handler(eventData.Data, cancellationToken);
                    }

                    consumer.Commit(consumeResult);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao processar mensagem Kafka: {ex.Message}");
                    // Não fazer commit em caso de erro
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Consumo cancelado, normal
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro no loop de consumo Kafka: {ex.Message}");
        }
        finally
        {
            consumer.Close();
        }
    }

    private string GenerateTopicName<TEvent>() where TEvent : IDomainEvent
    {
        var instance = Activator.CreateInstance<TEvent>();
        var prefix = _settings.Events.TopicPrefix;
        var eventType = instance.EventType.Replace('.', '-');
        return $"{prefix}-{eventType}".ToLowerInvariant();
    }

    private class EventWrapper<T>
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public DateTime OccurredOn { get; set; }
        public string? CorrelationId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public T? Data { get; set; }
    }

    public void Dispose()
    {
        foreach (var cts in _cancellationTokens.Values)
        {
            cts.Cancel();
        }

        foreach (var consumer in _consumers.Values)
        {
            consumer.Close();
            consumer.Dispose();
        }

        _consumers.Clear();
        _cancellationTokens.Clear();
    }
}


