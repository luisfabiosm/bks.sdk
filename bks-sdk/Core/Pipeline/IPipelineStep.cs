using bks.sdk.Common.Results;


namespace bks.sdk.Core.Pipeline;


public interface IPipelineStep<TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    string StepName { get; }
    int Order { get; }
}

public abstract class BasePipelineStep<TRequest, TResponse> : IPipelineStep<TRequest, TResponse>
{
    public abstract string StepName { get; }
    public abstract int Order { get; }
    public abstract Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    protected virtual Task OnStepStarting(TRequest request) => Task.CompletedTask;
    protected virtual Task OnStepCompleted(TRequest request, Result<TResponse> result) => Task.CompletedTask;
    protected virtual Task OnStepFailed(TRequest request, Exception exception) => Task.CompletedTask;
}