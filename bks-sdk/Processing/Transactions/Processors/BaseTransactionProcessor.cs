using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Processing.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Transactions.Processors;
public abstract class BaseTransactionProcessor<TTransaction, TResponse> : IBKSTransactionProcessor<TTransaction, TResponse>
    where TTransaction : BaseTransaction
{
    protected readonly IBKSLogger Logger;
    protected readonly IBKSTracer Tracer;

    public abstract string ProcessorName { get; }

    protected BaseTransactionProcessor(IBKSLogger logger, IBKSTracer tracer)
    {
        Logger = logger;
        Tracer = tracer;
    }

    public async Task<Result<TResponse>> ProcessAsync(TTransaction request, CancellationToken cancellationToken = default)
    {
        using var span = Tracer.StartSpan($"TransactionProcessor.{ProcessorName}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.Info($"Iniciando processamento da transação: {request.TransactionType} - ID: {request.Id}");

            await OnProcessing(request);

            var result = await ProcessTransactionAsync(request, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                Logger.Info($"Transação processada com sucesso: {request.Id} - Duração: {stopwatch.ElapsedMilliseconds}ms");
                await OnProcessed(request, result);
            }
            else
            {
                Logger.Warn($"Falha no processamento da transação: {request.Id} - Erro: {result.Error} - Duração: {stopwatch.ElapsedMilliseconds}ms");
                await OnFailed(request, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Error($"Exceção no processamento da transação: {request.Id} - Erro: {ex.Message} - Duração: {stopwatch.ElapsedMilliseconds}ms");

            await OnException(request, ex);
            return Result<TResponse>.Failure($"Erro no processamento da transação: {ex.Message}");
        }
    }

    protected abstract Task<Result<TResponse>> ProcessTransactionAsync(TTransaction transaction, CancellationToken cancellationToken);

    protected virtual Task OnProcessing(TTransaction transaction) => Task.CompletedTask;
    protected virtual Task OnProcessed(TTransaction transaction, Result<TResponse> result) => Task.CompletedTask;
    protected virtual Task OnFailed(TTransaction transaction, Result<TResponse> result) => Task.CompletedTask;
    protected virtual Task OnException(TTransaction transaction, Exception exception) => Task.CompletedTask;
}
