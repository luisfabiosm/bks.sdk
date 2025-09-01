using bks.sdk.Observability.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Events.Providers.RabbitMQ;


public class RabbitMQAsyncEventConsumer<TEvent> : IAsyncBasicConsumer
    where TEvent : IDomainEvent
{
    private readonly Func<TEvent, CancellationToken, Task> _bkshandler;
    private readonly IBKSLogger _bkslogger;
    private readonly IChannel _bkschannel;

    // Propriedade obrigatória da interface IAsyncBasicConsumer
    public IChannel Channel => _bkschannel;

    public event AsyncEventHandler<ConsumerEventArgs>? ConsumerCancelled;

    public RabbitMQAsyncEventConsumer(
        IChannel channel,
        Func<TEvent, CancellationToken, Task> handler,
        IBKSLogger logger)
    {
        _bkschannel = channel;
        _bkshandler = handler;
        _bkslogger = logger;
    }

    public async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = Encoding.UTF8.GetString(body.ToArray());
            var eventData = JsonSerializer.Deserialize<EventWrapper<TEvent>>(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (eventData.Data != null)
            {
                await _bkshandler(eventData.Data, cancellationToken);
            }

            // ACK da mensagem
            await _bkschannel.BasicAckAsync(deliveryTag, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _bkslogger.Error($"Erro ao processar mensagem RabbitMQ: {ex.Message}");

            // NACK da mensagem (rejeita sem requeue se já foi redelivered)
            await _bkschannel.BasicNackAsync(deliveryTag, false, !redelivered, cancellationToken);
        }
    }

    public async Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        _bkslogger.Warn($"RabbitMQ channel shutdown: {reason.ReplyText}");
        await Task.CompletedTask;
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

