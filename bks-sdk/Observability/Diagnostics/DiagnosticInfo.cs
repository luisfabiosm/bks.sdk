using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Diagnostics;



public record DiagnosticInfo
{
    public string ApplicationName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public TimeSpan Uptime { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public SystemMetrics System { get; init; } = new();
    public ApplicationMetrics Application { get; init; } = new();
}
