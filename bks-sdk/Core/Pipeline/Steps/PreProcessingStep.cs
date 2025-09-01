using bks.sdk.Common.Results;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Observability.Logging;


namespace bks.sdk.Core.Pipeline.Steps;

public class PreProcessingStep<TRequest, TResponse> : BasePipelineStep<TRequest, TResponse> where TRequest : class
    where TResponse : class
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IBKSLogger _logger;

    public override string StepName => "PreProcessing";
    public override int Order => 2;

    public PreProcessingStep(IEventPublisher eventPublisher, IBKSLogger logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public override async Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await OnStepStarting(request);

        try
        {
            _logger.Trace("Iniciando pré-processamento");

            // Publicar evento de processamento iniciado
            await _eventPublisher.PublishAsync(new TransactionProcessingEvent
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                TransactionType = typeof(TRequest).Name
            }, cancellationToken);

            // Aqui você pode adicionar lógica personalizada de pré-processamento
            // como preparação de dados, verificações de estado, etc.

            _logger.Trace("Pré-processamento concluído");

            var result = Result<TResponse>.Success(default(TResponse)!);
            await OnStepCompleted(request, result);
            return result;
        }
        catch (Exception ex)
        {
            await OnStepFailed(request, ex);
            throw;
        }
    }
}

