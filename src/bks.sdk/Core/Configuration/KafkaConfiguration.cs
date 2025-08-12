using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record KafkaConfiguration
    {
        public required string BootstrapServers { get; init; }
        public string? SecurityProtocol { get; init; }
        public string? SaslMechanism { get; init; }
        public string? SaslUsername { get; init; }
        public string? SaslPassword { get; init; }
        public Dictionary<string, string> AdditionalProperties { get; init; } = new();
    }
}
