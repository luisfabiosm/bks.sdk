using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Events;
using bks.sdk.Common.Results;
using bks.sdk.Enum;

namespace bks.sdk.Transactions;

public abstract class TransactionProcessor : ITransactionProcessor
{
    private readonly IBKSLogger _logger;
    private readonly IBKSTracer _tracer;
    private readonly IEventBroker _eventBroker;

    protected TransactionProcessor(
        IServiceProvider serviceProvider,
        IBKSLogger? logger,
        IBKSTracer? tracer,
        IEventBroker? eventBroker)
    {
        _logger = logger;
        _tracer = tracer;
        _eventBroker = eventBroker;
    }

    public async Task<Result> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        using var mainSpan = _tracer.StartSpan($"transaction-pipeline-{transaction.GetType().Name}");

        _logger.Info($"Iniciando pipeline de transação {transaction.CorrelationId}");

        try
        {
            // 1. Validação inicial
            var validationResult = await ValidateTransactionAsync(transaction, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                await OnProcessingFailedAsync(transaction, validationResult.Error!, cancellationToken);
                return validationResult;
            }

            // 2. Publicar evento de início
            await PublishTransactionStartedEventAsync(transaction);
            transaction.Status = TransactionStatus.PreProcessing;

            // 3. Pré-processamento
            var preProcessResult = await ExecutePreProcessingAsync(transaction, cancellationToken);
            if (!preProcessResult.IsSuccess)
            {
                await OnProcessingFailedAsync(transaction, preProcessResult.Error!, cancellationToken);
                return preProcessResult;
            }

            transaction.Status = TransactionStatus.Processing;

            // 4. Processamento principal
            var processResult = await ExecuteProcessingAsync(transaction, cancellationToken);
            if (!processResult.IsSuccess)
            {
                await OnProcessingFailedAsync(transaction, processResult.Error!, cancellationToken);
                return processResult;
            }

            transaction.Status = TransactionStatus.PostProcessing;

            // 5. Pós-processamento
            var postProcessResult = await ExecutePostProcessingAsync(transaction, cancellationToken);
            if (!postProcessResult.IsSuccess)
            {
                await OnProcessingFailedAsync(transaction, postProcessResult.Error!, cancellationToken);
                return postProcessResult;
            }

            // 6. Finalização com sucesso
            transaction.Status = TransactionStatus.Completed;
            await PublishTransactionCompletedEventAsync(transaction);

            _logger.Info($"Transação {transaction.CorrelationId} processada com sucesso");
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.Warn($"Transação {transaction.CorrelationId} foi cancelada");
            transaction.Status = TransactionStatus.Cancelled;
            await PublishTransactionCancelledEventAsync(transaction);
            return Result.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro inesperado na transação {transaction.CorrelationId}: {ex.Message}");
            await OnProcessingFailedAsync(transaction, ex.Message, cancellationToken);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }




    private async Task<Result> ValidateTransactionAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        using var span = _tracer.StartSpan("transaction-validation");

        // Validação de integridade
        if (!transaction.VerifyIntegrity())
        {
            return Result.Failure("Integridade da transação comprometida");
        }

        // Validação específica da transação
        var validationResult = transaction.ValidateTransaction();
        if (!validationResult.IsValid)
        {
            return Result.Failure(string.Join(", ", validationResult.Errors));
        }

        // Validação customizada (pode ser sobrescrita)
        return await ValidateAsync(transaction, cancellationToken);
    }


    private async Task<Result> ExecutePreProcessingAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        using var span = _tracer.StartSpan("transaction-pre-processing");

        _logger.Info($"Iniciando pré-processamento da transação {transaction.CorrelationId}");

        try
        {
            var result = await PreProcessAsync(transaction, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.Info($"Pré-processamento da transação {transaction.CorrelationId} concluído");
            }
            else
            {
                _logger.Warn($"Falha no pré-processamento da transação {transaction.CorrelationId}: {result.Error}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro no pré-processamento da transação {transaction.CorrelationId}: {ex.Message}");
            return Result.Failure($"Erro no pré-processamento: {ex.Message}");
        }
    }


    private async Task<Result> ExecuteProcessingAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        using var span = _tracer.StartSpan("transaction-processing");

        _logger.Info($"Iniciando processamento da transação {transaction.CorrelationId}");

        try
        {
            var result = await ProcessAsync(transaction, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.Info($"Processamento da transação {transaction.CorrelationId} concluído");
            }
            else
            {
                _logger.Warn($"Falha no processamento da transação {transaction.CorrelationId}: {result.Error}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro no processamento da transação {transaction.CorrelationId}: {ex.Message}");
            return Result.Failure($"Erro no processamento: {ex.Message}");
        }
    }


    private async Task<Result> ExecutePostProcessingAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        using var span = _tracer.StartSpan("transaction-post-processing");

        _logger.Info($"Iniciando pós-processamento da transação {transaction.CorrelationId}");

        try
        {
            var result = await PostProcessAsync(transaction, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.Info($"Pós-processamento da transação {transaction.CorrelationId} concluído");
            }
            else
            {
                _logger.Warn($"Falha no pós-processamento da transação {transaction.CorrelationId}: {result.Error}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro no pós-processamento da transação {transaction.CorrelationId}: {ex.Message}");
            return Result.Failure($"Erro no pós-processamento: {ex.Message}");
        }
    }

    private async Task OnProcessingFailedAsync(BaseTransaction transaction, string error, CancellationToken cancellationToken)
    {
        transaction.Status = TransactionStatus.Failed;
        transaction.AddMetadata("error", error);
        transaction.AddMetadata("failed_at", DateTime.UtcNow);

        await PublishTransactionFailedEventAsync(transaction, error);

        // Executar compensação se necessário
        try
        {
            await CompensateAsync(transaction, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro na compensação da transação {transaction.CorrelationId}: {ex.Message}");
        }
    }

    #region Métodos Virtuais para Sobrescrita


    protected virtual Task<Result> ValidateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }


    protected virtual Task<Result> PreProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }


    protected abstract Task<Result> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken);


    protected virtual Task<Result> PostProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }


    protected virtual Task CompensateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Eventos de Domínio

 
    protected virtual async Task PublishTransactionStartedEventAsync(BaseTransaction transaction)
    {
        await PublishDomainEventAsync(new TransactionStartedEvent
        {
            TransactionId = transaction.CorrelationId,
            TransactionType = transaction.GetType().Name,
            CreatedAt = transaction.CreatedAt
        });
    }


    protected virtual async Task PublishTransactionCompletedEventAsync(BaseTransaction transaction)
    {
        await PublishDomainEventAsync(new TransactionCompletedEvent
        {
            TransactionId = transaction.CorrelationId,
            TransactionType = transaction.GetType().Name,
            CompletedAt = DateTime.UtcNow
        });
    }


    protected virtual async Task PublishTransactionFailedEventAsync(BaseTransaction transaction, string error)
    {
        await PublishDomainEventAsync(new TransactionFailedEvent
        {
            TransactionId = transaction.CorrelationId,
            TransactionType = transaction.GetType().Name,
            Error = error,
            FailedAt = DateTime.UtcNow
        });
    }


    protected virtual async Task PublishTransactionCancelledEventAsync(BaseTransaction transaction)
    {
        await PublishDomainEventAsync(new TransactionCancelledEvent
        {
            TransactionId = transaction.CorrelationId,
            TransactionType = transaction.GetType().Name,
            CancelledAt = DateTime.UtcNow
        });
    }


    protected async Task PublishDomainEventAsync(IDomainEvent domainEvent)
    {
        try
        {
            await _eventBroker.PublishAsync(domainEvent);
            _logger.Info($"Evento {domainEvent.EventType} publicado para transação");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao publicar evento {domainEvent.EventType}: {ex.Message}");
        }
    }

    #endregion
}