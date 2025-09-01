using bks.sdk.Common.Results;
using bks.sdk.Core.Configuration;
using bks.sdk.Core.Pipeline.Steps;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace bks.sdk.Core.Pipeline;

public class PipelineExecutor : IPipelineExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;
    private readonly IBKSTracer _tracer;
    private readonly IEventPublisher _eventPublisher;

    public PipelineExecutor(
        IServiceProvider serviceProvider,
        BKSFrameworkSettings settings,
        IBKSLogger logger,
        IBKSTracer tracer,
        IEventPublisher eventPublisher)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
        _tracer = tracer;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<TResponse>> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class where TResponse : class
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var context = new PipelineContext<TRequest>(request, correlationId, cancellationToken);

        using var span = _tracer.StartSpan($"Pipeline.Execute.{typeof(TRequest).Name}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.Info($"Pipeline iniciada - CorrelationId: {correlationId}, Type: {typeof(TRequest).Name}");

            // Publicar evento de início se habilitado
            if (_settings.Processing.EnablePipelineEvents && _settings.Events.Enabled)
            {
                await _eventPublisher.PublishAsync(new TransactionStartedEvent
                {
                    TransactionId = correlationId,
                    TransactionType = typeof(TRequest).Name
                }, cancellationToken);
            }

            // Executar etapas da pipeline
            var result = await ExecutePipelineSteps<TRequest, TResponse>(context);

            stopwatch.Stop();

            // Publicar evento de conclusão
            if (_settings.Processing.EnablePipelineEvents && _settings.Events.Enabled)
            {
                if (result.IsSuccess)
                {
                    await _eventPublisher.PublishAsync(new TransactionCompletedEvent
                    {
                        TransactionId = correlationId,
                        TransactionType = typeof(TRequest).Name
                    }, cancellationToken);
                }
                else
                {
                    await _eventPublisher.PublishAsync(new TransactionFailedEvent
                    {
                        TransactionId = correlationId,
                        TransactionType = typeof(TRequest).Name,
                        Error = result.Error ?? "Erro desconhecido"
                    }, cancellationToken);
                }
            }

            _logger.Info($"Pipeline concluída - CorrelationId: {correlationId}, " +
                        $"Sucesso: {result.IsSuccess}, Duração: {stopwatch.ElapsedMilliseconds}ms");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.Error($"Pipeline falhou - CorrelationId: {correlationId}, Erro: {ex.Message}");

            // Publicar evento de falha
            if (_settings.Processing.EnablePipelineEvents && _settings.Events.Enabled)
            {
                await _eventPublisher.PublishAsync(new TransactionFailedEvent
                {
                    TransactionId = correlationId,
                    TransactionType = typeof(TRequest).Name,
                    Error = ex.Message
                }, cancellationToken);
            }

            return Result<TResponse>.Failure($"Erro na pipeline: {ex.Message}");
        }
    }

    private async Task<Result<TResponse>> ExecutePipelineSteps<TRequest, TResponse>(
        IPipelineContext<TRequest> context)
        where TRequest : class where TResponse : class
    {
        var steps = GetOrderedPipelineSteps<TRequest, TResponse>();

        foreach (var step in steps)
        {
            try
            {
                _logger.Trace($"Executando etapa: {step.StepName} - CorrelationId: {context.CorrelationId}");

                var result = await step.ExecuteAsync(context.Request, context.CancellationToken);

                if (!result.IsSuccess)
                {
                    _logger.Warn($"Etapa falhou: {step.StepName} - Erro: {result.Error} - CorrelationId: {context.CorrelationId}");
                    return result;
                }

                _logger.Trace($"Etapa concluída: {step.StepName} - CorrelationId: {context.CorrelationId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Exceção na etapa: {step.StepName} - Erro: {ex.Message} - CorrelationId: {context.CorrelationId}");
                return Result<TResponse>.Failure($"Falha na etapa {step.StepName}: {ex.Message}");
            }
        }

        // Se chegou até aqui, todas as etapas foram executadas com sucesso
        // O resultado final deve vir da etapa de processamento
        var processingStep = steps.FirstOrDefault(s => s.StepName.Contains("Processing"));
        if (processingStep != null)
        {
            return await processingStep.ExecuteAsync(context.Request, context.CancellationToken);
        }

        return Result<TResponse>.Failure("Nenhuma etapa de processamento encontrada");
    }

    private List<IPipelineStep<TRequest, TResponse>> GetOrderedPipelineSteps<TRequest, TResponse>()
        where TRequest : class where TResponse : class
    {
        var steps = new List<IPipelineStep<TRequest, TResponse>>();

        // 1. Validation Step
        if (_settings.Processing.ValidationEnabled)
        {
            var validationStep = _serviceProvider.GetService<ValidationStep<TRequest, TResponse>>();
            if (validationStep != null) steps.Add(validationStep);
        }

        // 2. PreProcessing Step
        var preProcessingStep = _serviceProvider.GetService<PreProcessingStep<TRequest, TResponse>>();
        if (preProcessingStep != null) steps.Add(preProcessingStep);

        // 3. Processing Step (obrigatória)
        var processingStep = _serviceProvider.GetService<ProcessingStep<TRequest, TResponse>>();
        if (processingStep != null) steps.Add(processingStep);

        // 4. PostProcessing Step
        var postProcessingStep = _serviceProvider.GetService<PostProcessingStep<TRequest, TResponse>>();
        if (postProcessingStep != null) steps.Add(postProcessingStep);

        return steps.OrderBy(s => s.Order).ToList();
    }
}


