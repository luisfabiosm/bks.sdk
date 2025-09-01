using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Validation.Abstractions;


namespace bks.sdk.Core.Pipeline.Steps;
public class ValidationStep<TRequest, TResponse> : BasePipelineStep<TRequest, TResponse>
{
    private readonly IValidator<TRequest> _validator;
    private readonly IBKSLogger _logger;

    public override string StepName => "Validation";
    public override int Order => 1;

    public ValidationStep(IValidator<TRequest> validator, IBKSLogger logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public override async Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await OnStepStarting(request);

        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                _logger.Warn($"Validação falhou: {errors}");

                var result = Result<TResponse>.Failure($"Validação falhou: {errors}");
                await OnStepCompleted(request, result);
                return result;
            }

            _logger.Trace("Validação concluída com sucesso");
            var successResult = Result<TResponse>.Success(default(TResponse)!);
            await OnStepCompleted(request, successResult);
            return successResult;
        }
        catch (Exception ex)
        {
            await OnStepFailed(request, ex);
            throw;
        }
    }
}

