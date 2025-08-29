using bks.sdk.Events.Abstractions;
using bks.sdk.Observability.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Events.Providers.RabbitMQ;


public class RabbitMQAsyncEventConsumer<TEvent> : AsyncEventingBasicConsumer
    where TEvent : IDomainEvent
{
    private readonly Func<TEvent, CancellationToken, Task> _handler;
    private readonly IBKSLogger _logger;

    public RabbitMQAsyncEventConsumer(
        IModel channel,
        Func<TEvent, CancellationToken, Task> handler,
        IBKSLogger logger)
        : base(channel)
    {
        _handler = handler;
        _logger = logger;
    }

    public override async Task HandleBasicDeliver(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IBasicProperties properties,
        ReadOnlyMemory<byte> body)
    {
        try
        {
            var message = Encoding.UTF8.GetString(body.ToArray());
            var eventData = JsonSerializer.Deserialize<EventWrapper<TEvent>>(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (eventData?.Data != null)
            {
                await _handler(eventData.Data, CancellationToken.None);
            }

            // ACK da mensagem
            Model.BasicAck(deliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao processar mensagem RabbitMQ: {ex.Message}");

            // NACK da mensagem (rejeita sem requeue se já foi redelivered)
            Model.BasicNack(deliveryTag, false, !redelivered);
        }
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
}
