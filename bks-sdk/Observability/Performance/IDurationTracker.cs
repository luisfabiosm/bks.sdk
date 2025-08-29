using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Performance;

public interface IDurationTracker : IDisposable
{
    string OperationName { get; }
    TimeSpan Elapsed { get; }
    Dictionary<string, object>? Tags { get; }
    void AddTag(string key, object value);
    void Complete();
    void CompleteWithError(Exception? exception = null);
}
