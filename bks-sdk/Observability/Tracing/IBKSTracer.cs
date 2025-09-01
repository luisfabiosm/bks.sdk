
using System.Diagnostics;

namespace bks.sdk.Observability.Tracing;


public interface IBKSTracer : IDisposable
{
    IDisposable StartSpan(string name);
    IDisposable StartSpan(string name, Dictionary<string, object>? tags);
    IDisposable StartSpan(string name, Activity? parent);
    IDisposable StartSpan(string name, Dictionary<string, object>? tags, Activity? parent);

    void AddTag(string key, object value);
    void AddEvent(string name);
    void AddEvent(string name, Dictionary<string, object>? attributes);
    void RecordException(Exception exception);

    Activity? CurrentActivity { get; }
    string? CurrentTraceId { get; }
    string? CurrentSpanId { get; }
}
