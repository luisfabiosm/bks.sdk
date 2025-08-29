using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;


public record BKSFrameworkSettings
{
    public string LicenseKey { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public ProcessingConfiguration Processing { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
    public EventsConfiguration Events { get; set; } = new();
    public ObservabilityConfiguration Observability { get; set; } = new();
}
