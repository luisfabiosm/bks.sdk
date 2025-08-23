using bks.sdk.Enum;

namespace bks.sdk.Core.Configuration;

public record SDKSettings
{
    public string LicenseKey { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public RedisSettings Redis { get; set; } = new();
    public JwtSettings Jwt { get; set; } = new();
    public EventBrokerSettings EventBroker { get; set; } = new();
    public ObservabilitySettings Observability { get; set; } = new();

    public SDKSettings()
    {
        
    }
}

public record RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public RedisSettings()
    {
        
    }
}

public record JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;

    public JwtSettings()
    {
        
    }
}

public record  EventBrokerSettings
{
    public EventBrokerType BrokerType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
    public EventBrokerSettings()
    {
        
    }
}

public record ObservabilitySettings
{
    public string ServiceName { get; set; } = "bks.sdk.Service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public OpenTelemetrySettings OpenTelemetry { get; set; } = new();

    public ObservabilitySettings()
    {
        
    }
}

public record OpenTelemetrySettings
{
    public string OtlpEndpoint { get; set; } = string.Empty;
    public bool EnableConsoleExporter { get; set; } = false;
    //public bool EnableJaegerExporter { get; set; } = false;
    public double TracingSampleRate { get; set; } = 1.0;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
    public ExporterSettings Exporters { get; set; } = new();

    public OpenTelemetrySettings()
    {
        
    }
}
public record ExporterSettings
{
    public OtlpExporterSettings Otlp { get; set; } = new();
    //public JaegerExporterSettings Jaeger { get; set; } = new();
    public bool EnableConsole { get; set; } = false;

    public ExporterSettings()
    {
        
    }
}

public record OtlpExporterSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Protocol { get; set; } = "grpc"; // grpc ou http/protobuf
    public int TimeoutMilliseconds { get; set; } = 10000;

    public OtlpExporterSettings()
    {
        
    }
}

//public class JaegerExporterSettings
//{
//    public string Endpoint { get; set; } = string.Empty;
//    public string AgentHost { get; set; } = "localhost";
//    public int AgentPort { get; set; } = 6831;
//}