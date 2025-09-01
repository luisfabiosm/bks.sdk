using bks.sdk.Common.Results;

namespace bks.sdk.Processing.Mediator.Abstractions;

public interface IBKSRequestHandler<TRequest, TResponse>
    where TRequest : IBKSRequest<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}