using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Correlation;

public class CorrelationContext
{
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime RequestStartTime { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}


