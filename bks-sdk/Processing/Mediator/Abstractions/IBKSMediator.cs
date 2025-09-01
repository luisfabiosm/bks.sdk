using bks.sdk.Common.Results;

namespace bks.sdk.Processing.Mediator.Abstractions;

public interface IBKSMediator
{
    Task<Result<TResponse>> SendAsync<TResponse>(IBKSRequest<TResponse> request, CancellationToken cancellationToken = default);
}