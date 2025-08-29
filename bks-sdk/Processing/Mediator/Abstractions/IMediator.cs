using bks.sdk.Common.Results;

namespace bks.sdk.Processing.Mediator.Abstractions;

public interface IMediator
{
    Task<Result<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}