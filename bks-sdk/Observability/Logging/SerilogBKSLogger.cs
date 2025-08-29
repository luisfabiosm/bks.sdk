using bks.sdk.Common.Enums;
using bks.sdk.Observability.Logging;
using Serilog;
using Serilog.Context;


namespace bks.sdk.Observability.Logging;


public class SerilogBKSLogger : IBKSLogger
{
    private readonly ILogger _logger;
    private readonly Microsoft.Extensions.Logging.ILogger _msLogger;

    public SerilogBKSLogger(ILogger logger, Microsoft.Extensions.Logging.ILogger<SerilogBKSLogger> msLogger)
    {
        _logger = logger;
        _msLogger = msLogger;
    }

    public void Trace(string message)
    {
        _logger.Verbose(message);
    }

    public void Trace(string message, params object[] args)
    {
        _logger.Verbose(message, args);
    }

    public void Debug(string message)
    {
        _logger.Debug(message);
    }

    public void Debug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Info(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void Warn(string message)
    {
        _logger.Warning(message);
    }

    public void Warn(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void Error(string message)
    {
        _logger.Error(message);
    }

    public void Error(string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    public void Error(Exception exception, string message)
    {
        _logger.Error(exception, message);
    }

    public void Error(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void Fatal(string message)
    {
        _logger.Fatal(message);
    }

    public void Fatal(Exception exception, string message)
    {
        _logger.Fatal(exception, message);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _msLogger.BeginScope(state);
    }

    public void LogWithCorrelation(LogLevel level, string message, string? correlationId = null)
    {
        using var context = !string.IsNullOrWhiteSpace(correlationId)
            ? LogContext.PushProperty("CorrelationId", correlationId)
            : null;

        switch (level)
        {
            case LogLevel.Trace:
                Trace(message);
                break;
            case LogLevel.Debug:
                Debug(message);
                break;
            case LogLevel.Information:
                Info(message);
                break;
            case LogLevel.Warning:
                Warn(message);
                break;
            case LogLevel.Error:
                Error(message);
                break;
            case LogLevel.Fatal:
                Fatal(message);
                break;
        }
    }

    public void LogStructured<T>(LogLevel level, string message, T data, string? correlationId = null)
    {
        using var correlationContext = !string.IsNullOrWhiteSpace(correlationId)
            ? LogContext.PushProperty("CorrelationId", correlationId)
            : null;

        using var dataContext = LogContext.PushProperty("Data", data, destructureObjects: true);

        switch (level)
        {
            case LogLevel.Trace:
                Trace(message);
                break;
            case LogLevel.Debug:
                Debug(message);
                break;
            case LogLevel.Information:
                Info(message);
                break;
            case LogLevel.Warning:
                Warn(message);
                break;
            case LogLevel.Error:
                Error(message);
                break;
            case LogLevel.Fatal:
                Fatal(message);
                break;
        }
    }
}

