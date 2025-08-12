using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record SerilogConfiguration
    {

        public string OutputTemplate { get; init; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";


        public bool EnableEnrichers { get; init; } = true;


        public string? LogFilePath { get; init; }


        public long MaxLogFileSizeMB { get; init; } = 100;

        public int MaxLogFiles { get; init; } = 10;
    }

}
