using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Pipeline;

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
