using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Events;
using bks.sdk.Common.Results;

namespace bks.sdk.Transactions;

public class TransactionProcessor : ITransactionProcessor
{
    private readonly ILogger _logger;
    private readonly ITracer _tracer;
    private readonly IEventBroker _eventBroker;

    public TransactionProcessor(ILogger logger, ITracer tracer, IEventBroker eventBroker)
    {
        _logger = logger;
        _tracer = tracer;
        _eventBroker = eventBroker;
    }

    public async Task<Result> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        using var span = _tracer.StartSpan("Transaction.Execute");
        try
        {
            _logger.Trace("Pré-processamento da transação.");
            await _eventBroker.PublishAsync(new TransactionStartedEvent(transaction.CorrelationId));

            var result = await ProcessAsync(transaction, cancellationToken);

            if (result.IsSuccess)
            {
                await _eventBroker.PublishAsync(new TransactionConfirmedEvent(transaction.CorrelationId));
                _logger.Info("Transação confirmada.");
            }
            else
            {
                await _eventBroker.PublishAsync(new TransactionCancelledEvent(transaction.CorrelationId));
                _logger.Warn("Transação cancelada.");
            }

            return result;
        }
        catch (Exception ex)
        {
            await _eventBroker.PublishAsync(new TransactionCancelledEvent(transaction.CorrelationId));
            _logger.Error("Erro na transação: " + ex.Message);
            return Result.Failure("Erro na transação: " + ex.Message);
        }
    }

    protected virtual Task<Result> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}

// Eventos de domínio para transações
public sealed class TransactionStartedEvent : DomainEvent
{
    public override string EventType => nameof(TransactionStartedEvent);
    public Guid CorrelationId { get; }

    public TransactionStartedEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}

public sealed class TransactionConfirmedEvent : DomainEvent 
{
    public override string EventType => nameof(TransactionConfirmedEvent);
    public Guid CorrelationId { get; }

    public TransactionConfirmedEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}

public sealed class TransactionCancelledEvent : DomainEvent
{
    public override string EventType => nameof(TransactionCancelledEvent);
    public Guid CorrelationId { get; }

    public TransactionCancelledEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
        OccurredOn = DateTime.UtcNow;
    }
}