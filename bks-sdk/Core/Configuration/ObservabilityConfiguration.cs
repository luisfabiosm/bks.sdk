using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record ObservabilityConfiguration
{
    public string ServiceName { get; set; } = "bks-framework-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public LoggingConfiguration Logging { get; set; } = new();
    public TracingConfiguration Tracing { get; set; } = new();
}
