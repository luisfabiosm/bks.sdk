using System;

namespace bks.sdk.Observability.Tracing;

public interface ITracer : IDisposable
{
    IDisposable StartSpan(string name);
}