using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Diagnostics;

public record ApplicationMetrics
{
    public long TotalRequests { get; init; }
    public long ActiveRequests { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public long ErrorCount { get; init; }
    public Dictionary<string, object> CustomMetrics { get; init; } = new();
}


