using OpenTelemetry.Trace;

namespace bks.sdk.Observability.Tracing;

public class OpenTelemetryTracer : IBKSTracer
{
    private readonly Tracer _tracer;

    public OpenTelemetryTracer(Tracer tracer)
    {
        _tracer = tracer;
    }

    public IDisposable StartSpan(string name)
    {
        var span = _tracer.StartActiveSpan(name);
        return span;
    }

    public void Dispose() { }
}