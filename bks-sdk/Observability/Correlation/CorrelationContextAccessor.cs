using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Correlation;


public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContext> _correlationContext = new();

    public string? CorrelationId
    {
        get => GetContext().CorrelationId;
        set => GetContext().CorrelationId = value;
    }

    public string? UserId
    {
        get => GetContext().UserId;
        set => GetContext().UserId = value;
    }

    public string? UserName
    {
        get => GetContext().UserName;
        set => GetContext().UserName = value;
    }

    public string? IpAddress
    {
        get => GetContext().IpAddress;
        set => GetContext().IpAddress = value;
    }

    public string? UserAgent
    {
        get => GetContext().UserAgent;
        set => GetContext().UserAgent = value;
    }

    public DateTime RequestStartTime
    {
        get => GetContext().RequestStartTime;
        set => GetContext().RequestStartTime = value;
    }

    public Dictionary<string, object> Properties => GetContext().Properties;

    public void SetProperty(string key, object value)
    {
        GetContext().Properties[key] = value;
    }

    public T? GetProperty<T>(string key)
    {
        var properties = GetContext().Properties;
        if (properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void ClearProperties()
    {
        GetContext().Properties.Clear();
    }

    private CorrelationContext GetContext()
    {
        if (_correlationContext.Value == null)
        {
            _correlationContext.Value = new CorrelationContext();
        }
        return _correlationContext.Value;
    }
}


