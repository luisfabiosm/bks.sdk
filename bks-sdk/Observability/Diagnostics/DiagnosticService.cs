using bks.sdk.Observability.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Diagnostics;



public class DiagnosticService : IDiagnosticService
{
    private readonly IBKSLogger _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static long _totalRequests = 0;
    private static long _activeRequests = 0;
    private static long _errorCount = 0;
    private static readonly List<double> _responseTimes = new();

    public DiagnosticService(IBKSLogger logger)
    {
        _logger = logger;
    }

    public async Task<DiagnosticInfo> GetDiagnosticInfoAsync()
    {
        var systemMetrics = await GetSystemMetricsAsync();
        var applicationMetrics = await GetApplicationMetricsAsync();

        return new DiagnosticInfo
        {
            ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown",
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            StartTime = _startTime,
            Uptime = DateTime.UtcNow - _startTime,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            System = systemMetrics,
            Application = applicationMetrics
        };
    }

    public async Task<SystemMetrics> GetSystemMetricsAsync()
    {
        await Task.CompletedTask;

        var process = Process.GetCurrentProcess();

        return new SystemMetrics
        {
            WorkingSetBytes = process.WorkingSet64,
            CpuUsagePercent = GetCpuUsage(),
            ThreadCount = process.Threads.Count,
            GcTotalMemory = GC.GetTotalMemory(false),
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2)
        };
    }

    public async Task<ApplicationMetrics> GetApplicationMetricsAsync()
    {
        await Task.CompletedTask;

        double averageResponseTime = 0;
        lock (_responseTimes)
        {
            if (_responseTimes.Count > 0)
            {
                averageResponseTime = _responseTimes.Average();
            }
        }

        return new ApplicationMetrics
        {
            TotalRequests = _totalRequests,
            ActiveRequests = _activeRequests,
            AverageResponseTimeMs = averageResponseTime,
            ErrorCount = _errorCount
        };
    }

    public static void IncrementRequestCount()
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _activeRequests);
    }

    public static void DecrementActiveRequests()
    {
        Interlocked.Decrement(ref _activeRequests);
    }

    public static void IncrementErrorCount()
    {
        Interlocked.Increment(ref _errorCount);
    }

    public static void RecordResponseTime(double milliseconds)
    {
        lock (_responseTimes)
        {
            _responseTimes.Add(milliseconds);

            // Manter apenas as últimas 1000 medições
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveAt(0);
            }
        }
    }

    private double GetCpuUsage()
    {
        // Implementação simplificada - em produção, use bibliotecas mais robustas
        try
        {
            var process = Process.GetCurrentProcess();
            return (process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount) * 100;
        }
        catch
        {
            return 0;
        }
    }
}

