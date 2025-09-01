using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;


namespace bks.sdk.Core.Pipeline.Steps;

public class PostProcessingStep<TRequest, TResponse> : BasePipelineStep<TRequest, TResponse>
{
    private readonly IBKSLogger _logger;

    public override string StepName => "PostProcessing";
    public override int Order => 4;

    public PostProcessingStep(IBKSLogger logger)
    {
        _logger = logger;
    }

    public override async Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await OnStepStarting(request);

        try
        {
            _logger.Trace("Iniciando pós-processamento");

            // Aqui você pode adicionar lógica de pós-processamento como:
            // - Limpeza de recursos
            // - Atualização de caches
            // - Notificações
            // - Auditoria adicional

            _logger.Trace("Pós-processamento concluído");

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

