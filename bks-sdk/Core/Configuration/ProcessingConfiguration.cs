using bks.sdk.Core.Configuration.bks.sdk.Core.Pipeline.bks.sdk.Core.Pipeline.bks.sdk.Core.Pipeline.bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Extensions.bks.sdk.Core.Initialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;


public record ProcessingConfiguration
{
    public ProcessingMode Mode { get; set; } = ProcessingMode.Mediator;
    public bool EnablePipelineEvents { get; set; } = true;
    public bool ValidationEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}
