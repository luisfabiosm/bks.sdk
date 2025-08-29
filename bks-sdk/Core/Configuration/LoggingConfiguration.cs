using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record LoggingConfiguration
{
    public string Level { get; set; } = "Information";
    public bool IncludeScopes { get; set; } = true;
    public bool WriteToConsole { get; set; } = true;
    public bool WriteToFile { get; set; } = true;
    public string FilePath { get; set; } = "logs/bks-framework-.log";
}