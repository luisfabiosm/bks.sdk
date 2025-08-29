using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Performance;

public interface IPerformanceTracker
{
    IDisposable TrackDuration(string operationName);
    IDisposable TrackDuration(string operationName, Dictionary<string, object>? tags);
    void TrackCounter(string counterName, long value = 1);
    void TrackCounter(string counterName, Dictionary<string, object>? tags, long value = 1);
    void TrackGauge(string gaugeName, double value);
    void TrackGauge(string gaugeName, Dictionary<string, object>? tags, double value);
    void TrackHistogram(string histogramName, double value);
    void TrackHistogram(string histogramName, Dictionary<string, object>? tags, double value);
}

