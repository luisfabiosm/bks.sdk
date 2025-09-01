using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Processing.Mediator.Abstractions;
using System.Diagnostics;



namespace bks.sdk.Processing.Mediator.Handlers;

public abstract class BaseRequestHandler<TRequest, TResponse> : IBKSRequestHandler<TRequest, TResponse>
    where TRequest : IBKSRequest<TResponse>
{
    protected readonly IBKSLogger Logger;
    protected readonly IBKSTracer Tracer;

    protected BaseRequestHandler(IBKSLogger logger, IBKSTracer tracer)
    {
        Logger = logger;
        Tracer = tracer;
    }

    public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var handlerName = GetType().Name;
        using var span = Tracer.StartSpan($"Handler.{handlerName}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.Info($"Iniciando handler: {handlerName}");

            await OnHandling(request);

            var result = await ProcessAsync(request, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                Logger.Info($"Handler concluído com sucesso: {handlerName} - Duração: {stopwatch.ElapsedMilliseconds}ms");
                await OnHandled(request, result);
            }
            else
            {
                Logger.Warn($"Handler falhou: {handlerName} - Erro: {result.Error} - Duração: {stopwatch.ElapsedMilliseconds}ms");
                await OnFailed(request, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Error($"Exceção no handler: {handlerName} - Erro: {ex.Message} - Duração: {stopwatch.ElapsedMilliseconds}ms");

            await OnException(request, ex);
            return Result<TResponse>.Failure($"Erro no handler {handlerName}: {ex.Message}");
        }
    }

    protected abstract Task<Result<TResponse>> ProcessAsync(TRequest request, CancellationToken cancellationToken);

    protected virtual Task OnHandling(TRequest request) => Task.CompletedTask;
    protected virtual Task OnHandled(TRequest request, Result<TResponse> result) => Task.CompletedTask;
    protected virtual Task OnFailed(TRequest request, Result<TResponse> result) => Task.CompletedTask;
    protected virtual Task OnException(TRequest request, Exception exception) => Task.CompletedTask;
}

