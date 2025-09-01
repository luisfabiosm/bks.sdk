using bks.sdk.Common.Enums;

namespace bks.sdk.Observability.Logging;

public interface IBKSLogger
{
    void Trace(string message);
    void Trace(string message, params object[] args);
    void Debug(string message);
    void Debug(string message, params object[] args);
    void Info(string message);
    void Info(string message, params object[] args);
    void Warn(string message);
    void Warn(string message, params object[] args);
    void Error(string message);
    void Error(string message, params object[] args);
    void Error(Exception exception, string message);
    void Error(Exception exception, string message, params object[] args);
    void Fatal(string message);
    void Fatal(Exception exception, string message);

    IDisposable BeginScope<TState>(TState state);
    void LogWithCorrelation(LogLevel level, string message, string? correlationId = null);
    void LogStructured<T>(LogLevel level, string message, T data, string? correlationId = null);
}

