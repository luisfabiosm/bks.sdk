using System;

namespace bks.sdk.Observability.Tracing;

public interface IBKSTracer : IDisposable
{
    IDisposable StartSpan(string name);
}