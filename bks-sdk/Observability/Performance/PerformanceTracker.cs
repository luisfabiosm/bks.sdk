using bks.sdk.Common.Enums;
using bks.sdk.Observability.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Performance;
public class PerformanceTracker : IPerformanceTracker
{
    private readonly IBKSLogger _logger;

    public PerformanceTracker(IBKSLogger logger)
    {
        _logger = logger;
    }

    public IDisposable TrackDuration(string operationName)
    {
        return TrackDuration(operationName, null);
    }

    public IDisposable TrackDuration(string operationName, Dictionary<string, object>? tags)
    {
        return new DurationTracker(operationName, tags, _logger);
    }

    public void TrackCounter(string counterName, long value = 1)
    {
        TrackCounter(counterName, null, value);
    }

    public void TrackCounter(string counterName, Dictionary<string, object>? tags, long value = 1)
    {
        _logger.LogStructured(LogLevel.Information,
            "Performance Counter: {CounterName} = {Value}",
            new { CounterName = counterName, Value = value, Tags = tags });
    }

    public void TrackGauge(string gaugeName, double value)
    {
        TrackGauge(gaugeName, null, value);
    }

    public void TrackGauge(string gaugeName, Dictionary<string, object>? tags, double value)
    {
        _logger.LogStructured(LogLevel.Information,
            "Performance Gauge: {GaugeName} = {Value}",
            new { GaugeName = gaugeName, Value = value, Tags = tags });
    }

    public void TrackHistogram(string histogramName, double value)
    {
        TrackHistogram(histogramName, null, value);
    }

    public void TrackHistogram(string histogramName, Dictionary<string, object>? tags, double value)
    {
        _logger.LogStructured(LogLevel.Information,
            "Performance Histogram: {HistogramName} = {Value}",
            new { HistogramName = histogramName, Value = value, Tags = tags });
    }

    private class DurationTracker : IDurationTracker
    {
        private readonly Stopwatch _stopwatch;
        private readonly IBKSLogger _logger;
        private bool _completed;

        public string OperationName { get; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;
        public Dictionary<string, object>? Tags { get; private set; }

        public DurationTracker(string operationName, Dictionary<string, object>? tags, IBKSLogger logger)
        {
            OperationName = operationName;
            Tags = tags ?? new Dictionary<string, object>();
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();

            _logger.Trace($"Iniciando rastreamento de performance: {operationName}");
        }

        public void AddTag(string key, object value)
        {
            Tags ??= new Dictionary<string, object>();
            Tags[key] = value;
        }

        public void Complete()
        {
            if (_completed) return;

            _stopwatch.Stop();
            _completed = true;

            _logger.LogStructured(LogLevel.Information,
                "Performance Duration: {OperationName} completed in {Duration}ms",
                new
                {
                    OperationName,
                    Duration = _stopwatch.ElapsedMilliseconds,
                    Success = true,
                    Tags
                });
        }

        public void CompleteWithError(Exception? exception = null)
        {
            if (_completed) return;

            _stopwatch.Stop();
            _completed = true;

            var logData = new
            {
                OperationName,
                Duration = _stopwatch.ElapsedMilliseconds,
                Success = false,
                Error = exception?.Message,
                Tags
            };

            if (exception != null)
            {
                _logger.Error(exception, "Performance Duration: {OperationName} failed after {Duration}ms");
                _logger.LogStructured(LogLevel.Error, "Performance error details", logData);
            }
            else
            {
                _logger.LogStructured(LogLevel.Warning,
                    "Performance Duration: {OperationName} completed with error after {Duration}ms",
                    logData);
            }
        }

        public void Dispose()
        {
            if (!_completed)
            {
                Complete();
            }
        }
    }
}

