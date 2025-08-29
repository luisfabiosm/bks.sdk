using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Pipeline;

public interface IPipelineContext<TRequest>
{
    TRequest Request { get; }
    string CorrelationId { get; }
    DateTime StartedAt { get; }
    Dictionary<string, object> Properties { get; }
    CancellationToken CancellationToken { get; }
}

