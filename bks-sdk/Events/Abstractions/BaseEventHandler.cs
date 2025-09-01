using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Abstractions;

public abstract class BaseEventHandler<TEvent> : IEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    protected readonly IBKSLogger Logger;
    protected readonly IBKSTracer Tracer;

    protected BaseEventHandler(IBKSLogger logger, IBKSTracer tracer)
    {
        Logger = logger;
        Tracer = tracer;
    }

    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerName = GetType().Name;
        using var span = Tracer.StartSpan($"EventHandler.{handlerName}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.Info($"Processando evento: {domainEvent.EventType} - Handler: {handlerName} - EventId: {domainEvent.EventId}");

            await OnHandling(domainEvent);
            await ProcessEventAsync(domainEvent, cancellationToken);
            await OnHandled(domainEvent);

            stopwatch.Stop();
            Logger.Info($"Evento processado com sucesso: {domainEvent.EventId} - Handler: {handlerName} - Duração: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Error($"Erro ao processar evento: {domainEvent.EventId} - Handler: {handlerName} - Erro: {ex.Message} - Duração: {stopwatch.ElapsedMilliseconds}ms");

            await OnFailed(domainEvent, ex);
            throw;
        }
    }

    protected abstract Task ProcessEventAsync(TEvent domainEvent, CancellationToken cancellationToken);

    protected virtual Task OnHandling(TEvent domainEvent) => Task.CompletedTask;
    protected virtual Task OnHandled(TEvent domainEvent) => Task.CompletedTask;
    protected virtual Task OnFailed(TEvent domainEvent, Exception exception) => Task.CompletedTask;
}


