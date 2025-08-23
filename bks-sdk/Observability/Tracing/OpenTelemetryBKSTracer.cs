using OpenTelemetry.Trace;
using System.Diagnostics;

namespace bks.sdk.Observability.Tracing;

public class OpenTelemetryBKSTracer : IBKSTracer
{
    private static readonly ActivitySource ActivitySource = new("bks.sdk");
    private readonly TracerProvider _tracerProvider;

    public OpenTelemetryBKSTracer(TracerProvider tracerProvider)
    {
        _tracerProvider = tracerProvider;
    }

    public IDisposable StartSpan(string name)
    {
        var activity = ActivitySource.StartActivity(name);
        return new ActivityWrapper(activity);
    }

    public void Dispose()
    {
        ActivitySource?.Dispose();
        _tracerProvider?.Dispose();
    }

    private class ActivityWrapper : IDisposable
    {
        private readonly Activity? _activity;

        public ActivityWrapper(Activity? activity)
        {
            _activity = activity;
        }

        public void Dispose()
        {
            _activity?.Dispose();
        }
    }
}
