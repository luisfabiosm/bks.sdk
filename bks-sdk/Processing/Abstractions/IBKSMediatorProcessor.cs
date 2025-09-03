
namespace bks.sdk.Processing.Abstractions;

public interface IBKSMediatorProcessor<TRequest, TResponse> : IBKSBusinessProcessor<TRequest, TResponse>
    where TRequest : class
{
}