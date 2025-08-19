using Serilog;

public class SerilogLogger : bks.sdk.Observability.Logging.ILogger
{
    public void Info(string message) => Log.Information(message);
    public void Warn(string message) => Log.Warning(message);
    public void Error(string message) => Log.Error(message);
    public void Trace(string message) => Log.Debug(message);
}