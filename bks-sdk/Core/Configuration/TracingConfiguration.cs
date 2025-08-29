using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record TracingConfiguration
{
    public double SamplingRate { get; set; } = 1.0;
    public string OtlpEndpoint { get; set; } = string.Empty;
    public bool EnableConsoleExporter { get; set; } = false;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
}