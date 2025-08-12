using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record ObservabilityConfiguration
    {
  
        public bool EnableLogging { get; init; } = true;

     
        public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;


        public bool EnableTracing { get; init; } = true;

        public bool EnableMetrics { get; init; } = true;

 
        public string? OtlpEndpoint { get; init; }


        public Dictionary<string, string> OtlpHeaders { get; init; } = new();


        public string ServiceName { get; init; } = "BKS.SDK";

  
        public string ServiceVersion { get; init; } = "1.0.0";

       
        public double TracingSampleRate { get; init; } = 1.0;

        public SerilogConfiguration Serilog { get; init; } = new();
    }


}
