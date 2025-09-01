using bks.sdk.Common.Results;
using bks.sdk.Processing.Abstractions;
using bks.sdk.Processing.Mediator.Abstractions;


namespace bks.sdk.Processing.Mediator;

public class BKSMediatorProcessor<TRequest, TResponse> : IBKSMediatorProcessor<TRequest, TResponse>
    where TRequest : class, IBKSRequest<TResponse>
{
    private readonly IBKSMediator _mediator;

    public string ProcessorName => "MediatorProcessor";

    public BKSMediatorProcessor(IBKSMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<TResponse>> ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return await _mediator.SendAsync(request, cancellationToken);
    }
}
