using bks.sdk.Common.Results;
using bks.sdk.Events;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Metrics;
using bks.sdk.Observability.Tracing;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;

namespace bks.sdk.Transactions;

/// <summary>
/// Classe base para processadores de transação com instrumentação completa
/// </summary>
public abstract class TransactionProcessor : ITransactionProcessor
{
    private static readonly ActivitySource ActivitySource = new("bks.sdk.transactions");

    protected readonly IBKSLogger Logger;
    protected readonly IEventBroker EventBroker;
    protected readonly IBKSTracer Tracer;

    protected TransactionProcessor(IServiceProvider serviceProvider,
        IBKSLogger logger,
        IBKSTracer tracer,
        IEventBroker eventBroker)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EventBroker = eventBroker ?? throw new ArgumentNullException(nameof(eventBroker));
        Tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    /// <summary>
    /// Executa uma transação com instrumentação completa
    /// </summary>
    public async Task<Result> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            const string error = "Transaction cannot be null";
            Logger.Error(error);
            return Result.Failure(error);
        }

        var transactionType = transaction.GetType().Name;
        var stopwatch = Stopwatch.StartNew();

        // Criar span principal para toda a operação
        using var mainActivity = ActivitySource.StartActivity("transaction.execute");
        mainActivity?.SetTag("transaction.id", transaction.CorrelationId);
        mainActivity?.SetTag("transaction.type", transactionType);
        mainActivity?.SetTag("transaction.correlation_id", transaction.CorrelationId);

        try
        {
            Logger.Info($"Iniciando processamento da transação {transactionType} - CorrelationId: {transaction.CorrelationId}");

            // Registrar início nos metrics
            BKSMetrics.TransactionStarted.Add(1, new KeyValuePair<string, object?>("transaction.type", transactionType));
            BKSMetrics.ActiveTransactions.Add(1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            // Publicar evento de início
            await PublishTransactionStartedEvent(transaction);

            // Pipeline de processamento com instrumentação
            var result = await ExecuteInstrumentedPipeline(transaction, cancellationToken);

            stopwatch.Stop();

            // Registrar métricas finais
            var success = result.IsSuccess;
            var duration = stopwatch.Elapsed.TotalSeconds;

            BKSMetrics.TransactionDuration.Record(duration,
                new KeyValuePair<string, object?>("transaction.type", transactionType),
                new KeyValuePair<string, object?>("success", success));

            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            if (success)
            {
                BKSMetrics.TransactionCompleted.Add(1, new KeyValuePair<string, object?>("transaction.type", transactionType));

                Logger.Info($"Transação {transactionType} concluída com sucesso - CorrelationId: {transaction.CorrelationId} - Duração: {duration:F3}s");

                mainActivity?.SetStatus(ActivityStatusCode.Ok);
                await PublishTransactionCompletedEvent(transaction);
            }
            else
            {
                BKSMetrics.TransactionFailed.Add(1,
                    new KeyValuePair<string, object?>("transaction.type", transactionType),
                    new KeyValuePair<string, object?>("error", result.Error ?? "Unknown error"));

                Logger.Error($"Transação {transactionType} falhou - CorrelationId: {transaction.CorrelationId} - Erro: {result.Error} - Duração: {duration:F3}s");

                mainActivity?.SetStatus(ActivityStatusCode.Error, result.Error ?? "Transaction failed");
                await PublishTransactionFailedEvent(transaction, result.Error ?? "Unknown error");
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            Logger.Warn($"Transação {transactionType} cancelada - CorrelationId: {transaction.CorrelationId} - Duração: {stopwatch.Elapsed.TotalSeconds:F3}s");

            mainActivity?.SetStatus(ActivityStatusCode.Error, "Transaction cancelled");
            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            await PublishTransactionCancelledEvent(transaction);

            return Result.Failure("Transaction was cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.Error($"Erro inesperado na transação {transactionType} - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");

            mainActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            mainActivity?.RecordException(ex);

            BKSMetrics.TransactionFailed.Add(1,
                new KeyValuePair<string, object?>("transaction.type", transactionType),
                new KeyValuePair<string, object?>("error", "unexpected_exception"));
            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            await PublishTransactionFailedEvent(transaction, ex.Message);

            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Pipeline instrumentado de processamento
    /// </summary>
    private async Task<Result> ExecuteInstrumentedPipeline(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        // 1. Pré-processamento
        using (var preProcessActivity = ActivitySource.StartActivity("transaction.preprocess"))
        {
            preProcessActivity?.SetTag("transaction.id", transaction.CorrelationId);
            preProcessActivity?.SetTag("phase", "preprocess");

            Logger.Trace($"Iniciando pré-processamento - CorrelationId: {transaction.CorrelationId}");

            var preProcessResult = await PreProcessAsync(transaction, cancellationToken);
            if (!preProcessResult.IsSuccess)
            {
                preProcessActivity?.SetStatus(ActivityStatusCode.Error, preProcessResult.Error);
                return preProcessResult;
            }

            preProcessActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 2. Validação
        using (var validationActivity = ActivitySource.StartActivity("transaction.validate"))
        {
            validationActivity?.SetTag("transaction.id", transaction.CorrelationId);
            validationActivity?.SetTag("phase", "validation");

            Logger.Trace($"Iniciando validação - CorrelationId: {transaction.CorrelationId}");

            var validationResult = await ValidateAsync(transaction, cancellationToken);
            if (!validationResult.IsValid)
            {
                var error = $"Validation failed: {string.Join(", ", validationResult.Errors)}";
                validationActivity?.SetStatus(ActivityStatusCode.Error, error);
                return Result.Failure(error);
            }

            validationActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 3. Processamento principal
        using (var processActivity = ActivitySource.StartActivity("transaction.process"))
        {
            processActivity?.SetTag("transaction.id", transaction.CorrelationId);
            processActivity?.SetTag("phase", "process");

            Logger.Trace($"Iniciando processamento principal - CorrelationId: {transaction.CorrelationId}");

            var processResult = await ProcessAsync(transaction, cancellationToken);
            if (!processResult.IsSuccess)
            {
                processActivity?.SetStatus(ActivityStatusCode.Error, processResult.Error);
                return processResult;
            }

            processActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 4. Pós-processamento
        using (var postProcessActivity = ActivitySource.StartActivity("transaction.postprocess"))
        {
            postProcessActivity?.SetTag("transaction.id", transaction.CorrelationId);
            postProcessActivity?.SetTag("phase", "postprocess");

            Logger.Trace($"Iniciando pós-processamento - CorrelationId: {transaction.CorrelationId}");

            var postProcessResult = await PostProcessAsync(transaction, cancellationToken);
            if (!postProcessResult.IsSuccess)
            {
                postProcessActivity?.SetStatus(ActivityStatusCode.Error, postProcessResult.Error);
                return postProcessResult;
            }

            postProcessActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        return Result.Success();
    }

    /// <summary>
    /// Método abstrato que deve ser implementado pelas classes filhas
    /// </summary>
    protected abstract Task<Result> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Pré-processamento - pode ser sobrescrito pelas classes filhas
    /// </summary>
    protected virtual Task<Result> PreProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        Logger.Trace($"Pré-processamento padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Validação - pode ser sobrescrita pelas classes filhas
    /// </summary>
    protected virtual Task<ValidationResult> ValidateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        Logger.Trace($"Validação padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Pós-processamento - pode ser sobrescrito pelas classes filhas
    /// </summary>
    protected virtual Task<Result> PostProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        Logger.Trace($"Pós-processamento padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(Result.Success());
    }

    #region Event Publishing

    private async Task PublishTransactionStartedEvent(BaseTransaction transaction)
    {
        try
        {
            var startedEvent = new TransactionStartedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = transaction.GetType().Name,
                CreatedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(startedEvent);
            Logger.Trace($"Evento TransactionStarted publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionStarted - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionCompletedEvent(BaseTransaction transaction)
    {
        try
        {
            var completedEvent = new TransactionCompletedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = transaction.GetType().Name,
                CompletedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(completedEvent);
            Logger.Trace($"Evento TransactionCompleted publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionCompleted - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionFailedEvent(BaseTransaction transaction, string error)
    {
        try
        {
            var failedEvent = new TransactionFailedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = transaction.GetType().Name,
                Error = error,
                FailedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(failedEvent);
            Logger.Trace($"Evento TransactionFailed publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionFailed - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionCancelledEvent(BaseTransaction transaction)
    {
        try
        {
            var cancelledEvent = new TransactionCancelledEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = transaction.GetType().Name,
                CancelledAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(cancelledEvent);
            Logger.Trace($"Evento TransactionCancelled publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionCancelled - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Classe base genérica para processadores de transação com resultado tipado e instrumentação completa
/// </summary>
public abstract class TransactionProcessor<TResult> : ITransactionProcessor<TResult>
{
    private static readonly ActivitySource ActivitySource = new("bks.sdk.transactions");

    protected readonly IBKSLogger Logger;
    protected readonly IEventBroker EventBroker;
    protected readonly IBKSTracer Tracer;


    protected TransactionProcessor(
        IServiceProvider serviceProvider,
        IBKSLogger logger,
        IBKSTracer tracer,   
        IEventBroker eventBroker)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EventBroker = eventBroker ?? throw new ArgumentNullException(nameof(eventBroker));
        Tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    /// <summary>
    /// Verifica se este processador pode processar a transação especificada
    /// </summary>
    public abstract bool CanProcess(BaseTransaction transaction);

    /// <summary>
    /// Executa uma transação com resultado tipado e instrumentação completa
    /// </summary>
    public async Task<Result<TResult>> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
        {
            const string error = "Transaction cannot be null";
            Logger.Error(error);
            return Result<TResult>.Failure(error);
        }

        if (!CanProcess(transaction))
        {
            const string error = "This processor cannot handle the specified transaction type";
            Logger.Error($"{error} - Transaction: {transaction.GetType().Name}");
            return Result<TResult>.Failure(error);
        }

        var transactionType = transaction.GetType().Name;
        var resultType = typeof(TResult).Name;
        var stopwatch = Stopwatch.StartNew();

        // Criar span principal para toda a operação
        using var mainActivity = ActivitySource.StartActivity("transaction.execute_typed");
        mainActivity?.SetTag("transaction.id", transaction.CorrelationId);
        mainActivity?.SetTag("transaction.type", transactionType);
        mainActivity?.SetTag("transaction.correlation_id", transaction.CorrelationId);
        mainActivity?.SetTag("result.type", resultType);

        try
        {
            Logger.Info($"Iniciando processamento da transação tipada {transactionType}<{resultType}> - CorrelationId: {transaction.CorrelationId}");

            // Registrar início nos metrics
            BKSMetrics.TransactionStarted.Add(1,
                new KeyValuePair<string, object?>("transaction.type", transactionType),
                new KeyValuePair<string, object?>("result.type", resultType));
            BKSMetrics.ActiveTransactions.Add(1,
                new KeyValuePair<string, object?>("transaction.type", transactionType));

            // Publicar evento de início
            await PublishTransactionStartedEvent(transaction);

            // Pipeline de processamento com instrumentação
            var result = await ExecuteInstrumentedPipeline(transaction, cancellationToken);

            stopwatch.Stop();

            // Registrar métricas finais
            var success = result.IsSuccess;
            var duration = stopwatch.Elapsed.TotalSeconds;

            BKSMetrics.TransactionDuration.Record(duration,
                new KeyValuePair<string, object?>("transaction.type", transactionType),
                new KeyValuePair<string, object?>("result.type", resultType),
                new KeyValuePair<string, object?>("success", success));

            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            if (success)
            {
                BKSMetrics.TransactionCompleted.Add(1,
                    new KeyValuePair<string, object?>("transaction.type", transactionType),
                    new KeyValuePair<string, object?>("result.type", resultType));

                Logger.Info($"Transação tipada {transactionType}<{resultType}> concluída com sucesso - CorrelationId: {transaction.CorrelationId} - Duração: {duration:F3}s");

                mainActivity?.SetStatus(ActivityStatusCode.Ok);
                await PublishTransactionCompletedEvent(transaction);
            }
            else
            {
                BKSMetrics.TransactionFailed.Add(1,
                    new KeyValuePair<string, object?>("transaction.type", transactionType),
                    new KeyValuePair<string, object?>("result.type", resultType),
                    new KeyValuePair<string, object?>("error", result.Error ?? "Unknown error"));

                Logger.Error($"Transação tipada {transactionType}<{resultType}> falhou - CorrelationId: {transaction.CorrelationId} - Erro: {result.Error} - Duração: {duration:F3}s");

                mainActivity?.SetStatus(ActivityStatusCode.Error, result.Error ?? "Transaction failed");
                await PublishTransactionFailedEvent(transaction, result.Error ?? "Unknown error");
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            Logger.Warn($"Transação tipada {transactionType}<{resultType}> cancelada - CorrelationId: {transaction.CorrelationId} - Duração: {stopwatch.Elapsed.TotalSeconds:F3}s");

            mainActivity?.SetStatus(ActivityStatusCode.Error, "Transaction cancelled");
            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            await PublishTransactionCancelledEvent(transaction);

            return Result<TResult>.Failure("Transaction was cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Logger.Error($"Erro inesperado na transação tipada {transactionType}<{resultType}> - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");

            mainActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            mainActivity?.RecordException(ex);

            BKSMetrics.TransactionFailed.Add(1,
                new KeyValuePair<string, object?>("transaction.type", transactionType),
                new KeyValuePair<string, object?>("result.type", resultType),
                new KeyValuePair<string, object?>("error", "unexpected_exception"));
            BKSMetrics.ActiveTransactions.Add(-1, new KeyValuePair<string, object?>("transaction.type", transactionType));

            await PublishTransactionFailedEvent(transaction, ex.Message);

            return Result<TResult>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Pipeline instrumentado de processamento com resultado tipado
    /// </summary>
    private async Task<Result<TResult>> ExecuteInstrumentedPipeline(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        // 1. Pré-processamento
        using (var preProcessActivity = ActivitySource.StartActivity("transaction.preprocess"))
        {
            preProcessActivity?.SetTag("transaction.id", transaction.CorrelationId);
            preProcessActivity?.SetTag("phase", "preprocess");
            preProcessActivity?.SetTag("result.type", typeof(TResult).Name);

            Logger.Trace($"Iniciando pré-processamento tipado - CorrelationId: {transaction.CorrelationId}");

            var preProcessResult = await PreProcessAsync(transaction, cancellationToken);
            if (!preProcessResult.IsSuccess)
            {
                preProcessActivity?.SetStatus(ActivityStatusCode.Error, preProcessResult.Error);
                return Result<TResult>.Failure(preProcessResult.Error);
            }

            preProcessActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 2. Validação
        using (var validationActivity = ActivitySource.StartActivity("transaction.validate"))
        {
            validationActivity?.SetTag("transaction.id", transaction.CorrelationId);
            validationActivity?.SetTag("phase", "validation");
            validationActivity?.SetTag("result.type", typeof(TResult).Name);

            Logger.Trace($"Iniciando validação tipada - CorrelationId: {transaction.CorrelationId}");

            var validationResult = await ValidateAsync(transaction, cancellationToken);
            if (!validationResult.IsValid)
            {
                var error = $"Validation failed: {string.Join(", ", validationResult.Errors)}";
                validationActivity?.SetStatus(ActivityStatusCode.Error, error);
                return Result<TResult>.Failure(error);
            }

            validationActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // 3. Processamento principal
        using (var processActivity = ActivitySource.StartActivity("transaction.process"))
        {
            processActivity?.SetTag("transaction.id", transaction.CorrelationId);
            processActivity?.SetTag("phase", "process");
            processActivity?.SetTag("result.type", typeof(TResult).Name);

            Logger.Trace($"Iniciando processamento principal tipado - CorrelationId: {transaction.CorrelationId}");

            var processResult = await ProcessAsync(transaction, cancellationToken);
            if (!processResult.IsSuccess)
            {
                processActivity?.SetStatus(ActivityStatusCode.Error, processResult.Error);
                return processResult;
            }

            // Adicionar informações sobre o resultado ao span
            if (processResult.Value != null)
            {
                try
                {
                    var resultJson = JsonSerializer.Serialize(processResult.Value);
                    processActivity?.SetTag("result.value", resultJson);
                }
                catch
                {
                    processActivity?.SetTag("result.value", processResult.Value.ToString());
                }
            }

            processActivity?.SetStatus(ActivityStatusCode.Ok);

            // Continuar com o resultado do processamento
            var typedResult = processResult;

            // 4. Pós-processamento
            using (var postProcessActivity = ActivitySource.StartActivity("transaction.postprocess"))
            {
                postProcessActivity?.SetTag("transaction.id", transaction.CorrelationId);
                postProcessActivity?.SetTag("phase", "postprocess");
                postProcessActivity?.SetTag("result.type", typeof(TResult).Name);

                Logger.Trace($"Iniciando pós-processamento tipado - CorrelationId: {transaction.CorrelationId}");

                var postProcessResult = await PostProcessAsync(transaction, typedResult.Value, cancellationToken);
                if (!postProcessResult.IsSuccess)
                {
                    postProcessActivity?.SetStatus(ActivityStatusCode.Error, postProcessResult.Error);
                    return Result<TResult>.Failure(postProcessResult.Error);
                }

                postProcessActivity?.SetStatus(ActivityStatusCode.Ok);
            }

            return typedResult;
        }
    }

    /// <summary>
    /// Método abstrato que deve ser implementado pelas classes filhas
    /// </summary>
    protected abstract Task<Result<TResult>> ProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Pré-processamento - pode ser sobrescrito pelas classes filhas
    /// </summary>
    protected virtual Task<Result> PreProcessAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        Logger.Trace($"Pré-processamento tipado padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Validação - pode ser sobrescrita pelas classes filhas
    /// </summary>
    protected virtual Task<ValidationResult> ValidateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        Logger.Trace($"Validação tipada padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Pós-processamento - pode ser sobrescrito pelas classes filhas
    /// </summary>
    protected virtual Task<Result> PostProcessAsync(BaseTransaction transaction, TResult? result, CancellationToken cancellationToken)
    {
        Logger.Trace($"Pós-processamento tipado padrão - CorrelationId: {transaction.CorrelationId}");
        return Task.FromResult(Result.Success());
    }


    protected virtual async Task<Result> CompensateAsync(BaseTransaction transaction, CancellationToken cancellationToken)
    {
        return Result.Success();
    }

    #region Event Publishing

    private async Task PublishTransactionStartedEvent(BaseTransaction transaction)
    {
        try
        {
            var startedEvent = new TransactionStartedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = $"{transaction.GetType().Name}<{typeof(TResult).Name}>",
                CreatedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(startedEvent);
            Logger.Trace($"Evento TransactionStarted tipado publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionStarted tipado - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionCompletedEvent(BaseTransaction transaction)
    {
        try
        {
            var completedEvent = new TransactionCompletedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = $"{transaction.GetType().Name}<{typeof(TResult).Name}>",
                CompletedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(completedEvent);
            Logger.Trace($"Evento TransactionCompleted tipado publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionCompleted tipado - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionFailedEvent(BaseTransaction transaction, string error)
    {
        try
        {
            var failedEvent = new TransactionFailedEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = $"{transaction.GetType().Name}<{typeof(TResult).Name}>",
                Error = error,
                FailedAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(failedEvent);
            Logger.Trace($"Evento TransactionFailed tipado publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionFailed tipado - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    private async Task PublishTransactionCancelledEvent(BaseTransaction transaction)
    {
        try
        {
            var cancelledEvent = new TransactionCancelledEvent
            {
                TransactionId = transaction.CorrelationId,
                TransactionType = $"{transaction.GetType().Name}<{typeof(TResult).Name}>",
                CancelledAt = DateTime.UtcNow
            };

            await EventBroker.PublishAsync(cancelledEvent);
            Logger.Trace($"Evento TransactionCancelled tipado publicado - CorrelationId: {transaction.CorrelationId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao publicar evento TransactionCancelled tipado - CorrelationId: {transaction.CorrelationId} - Erro: {ex.Message}");
        }
    }

    #endregion
}