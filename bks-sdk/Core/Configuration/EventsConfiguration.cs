using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record EventsConfiguration
{
    public bool Enabled { get; set; } = false;
    public EventProvider Provider { get; set; } = EventProvider.InMemory;
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicPrefix { get; set; } = "bks-framework";
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}