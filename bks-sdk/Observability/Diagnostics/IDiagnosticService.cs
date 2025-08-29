using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Diagnostics;


public interface IDiagnosticService
{
    Task<DiagnosticInfo> GetDiagnosticInfoAsync();
    Task<SystemMetrics> GetSystemMetricsAsync();
    Task<ApplicationMetrics> GetApplicationMetricsAsync();
}
