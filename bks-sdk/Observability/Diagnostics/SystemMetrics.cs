using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Diagnostics;


public record SystemMetrics
{
    public long WorkingSetBytes { get; init; }
    public double CpuUsagePercent { get; init; }
    public int ThreadCount { get; init; }
    public long GcTotalMemory { get; init; }
    public int GcGen0Collections { get; init; }
    public int GcGen1Collections { get; init; }
    public int GcGen2Collections { get; init; }
}
