using bks.sdk.Common.Results;

namespace bks.sdk.Processing.Mediator.Abstractions;

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}