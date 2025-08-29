using bks.sdk.Common.Enums;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Tracing;


public class OpenTelemetryBKSTracer : IBKSTracer
{
    private static readonly ActivitySource ActivitySource = new("bks.sdk");
    private readonly TracerProvider _tracerProvider;

    public OpenTelemetryBKSTracer(TracerProvider tracerProvider)
    {
        _tracerProvider = tracerProvider;
    }

    public Activity? CurrentActivity => Activity.Current;
    public string? CurrentTraceId => Activity.Current?.TraceId.ToString();
    public string? CurrentSpanId => Activity.Current?.SpanId.ToString();

    public IDisposable StartSpan(string name)
    {
        return StartSpan(name, null, null);
    }

    public IDisposable StartSpan(string name, Dictionary<string, object>? tags)
    {
        return StartSpan(name, tags, null);
    }

    public IDisposable StartSpan(string name, Activity? parent)
    {
        return StartSpan(name, null, parent);
    }

    public IDisposable StartSpan(string name, Dictionary<string, object>? tags, Activity? parent)
    {
        var activity = parent != null
            ? ActivitySource.StartActivity(name, ActivityKind.Internal, parent.Context)
            : ActivitySource.StartActivity(name);

        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value?.ToString());
            }
        }

        return new SpanContext(activity);
    }

    public void AddTag(string key, object value)
    {
        Activity.Current?.SetTag(key, value?.ToString());
    }

    public void AddEvent(string name)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name));
    }

    public void AddEvent(string name, Dictionary<string, object>? attributes)
    {
        if (attributes != null)
        {
            var activityTags = new ActivityTagsCollection();
            foreach (var attr in attributes)
            {
                activityTags.Add(attr.Key, attr.Value);
            }
            Activity.Current?.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, activityTags));
        }
        else
        {
            AddEvent(name);
        }
    }

    public void RecordException(Exception exception)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.RecordException(exception);
        }
    }

    public void Dispose()
    {
        ActivitySource?.Dispose();
        _tracerProvider?.Dispose();
    }

    private class SpanContext : ISpanContext
    {
        private readonly Activity? _activity;

        public SpanContext(Activity? activity)
        {
            _activity = activity;
        }

        public string SpanId => _activity?.SpanId.ToString() ?? string.Empty;
        public string TraceId => _activity?.TraceId.ToString() ?? string.Empty;
        public Activity Activity => _activity ?? throw new InvalidOperationException("Activity is null");

        public void AddTag(string key, object value)
        {
            _activity?.SetTag(key, value?.ToString());
        }

        public void AddEvent(string name)
        {
            _activity?.AddEvent(new ActivityEvent(name));
        }

        public void AddEvent(string name, Dictionary<string, object>? attributes)
        {
            if (attributes != null)
            {
                var activityTags = new ActivityTagsCollection();
                foreach (var attr in attributes)
                {
                    activityTags.Add(attr.Key, attr.Value);
                }
                _activity?.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, activityTags));
            }
            else
            {
                AddEvent(name);
            }
        }

        public void RecordException(Exception exception)
        {
            if (_activity != null)
            {
                _activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                _activity.RecordException(exception);
            }
        }

        public void SetStatus(SpanStatus status, string? description = null)
        {
            if (_activity != null)
            {
                var activityStatus = status == SpanStatus.Ok
                    ? ActivityStatusCode.Ok
                    : ActivityStatusCode.Error;

                _activity.SetStatus(activityStatus, description);
            }
        }

        public void Dispose()
        {
            _activity?.Dispose();
        }
    }
}


