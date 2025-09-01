using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Correlation;

public interface ICorrelationContextAccessor
{
    string? CorrelationId { get; set; }
    string? UserId { get; set; }
    string? UserName { get; set; }
    string? IpAddress { get; set; }
    string? UserAgent { get; set; }
    DateTime RequestStartTime { get; set; }
    Dictionary<string, object> Properties { get; }

    void SetProperty(string key, object value);
    T? GetProperty<T>(string key);
    void ClearProperties();
}

