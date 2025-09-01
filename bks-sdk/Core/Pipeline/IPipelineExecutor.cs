using bks.sdk.Common.Results;


namespace bks.sdk.Core.Pipeline;

public interface IPipelineExecutor
{
    Task<Result<TResponse>> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class where TResponse : class;
}

public interface IPipelineContext<TRequest>
{
    TRequest Request { get; }
    string CorrelationId { get; }
    DateTime StartedAt { get; }
    Dictionary<string, object> Properties { get; }
    CancellationToken CancellationToken { get; }
}

public class PipelineContext<TRequest> : IPipelineContext<TRequest>
{
    public TRequest Request { get; }
    public string CorrelationId { get; }
    public DateTime StartedAt { get; }
    public Dictionary<string, object> Properties { get; }
    public CancellationToken CancellationToken { get; }

    public PipelineContext(TRequest request, string correlationId, CancellationToken cancellationToken)
    {
        Request = request;
        CorrelationId = correlationId;
        StartedAt = DateTime.UtcNow;
        Properties = new Dictionary<string, object>();
        CancellationToken = cancellationToken;
    }
}
