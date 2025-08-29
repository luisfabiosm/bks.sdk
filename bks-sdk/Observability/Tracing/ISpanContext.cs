﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Tracing;

public interface ISpanContext : IDisposable
{
    string SpanId { get; }
    string TraceId { get; }
    Activity Activity { get; }

    void AddTag(string key, object value);
    void AddEvent(string name);
    void AddEvent(string name, Dictionary<string, object>? attributes);
    void RecordException(Exception exception);
    void SetStatus(SpanStatus status, string? description = null);
}

