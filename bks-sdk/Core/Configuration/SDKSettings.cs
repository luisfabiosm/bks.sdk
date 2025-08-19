using bks.sdk.Enum;

namespace bks.sdk.Core.Configuration;

public class SDKSettings
{
    public string LicenseKey { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public RedisSettings Redis { get; set; } = new();
    public JwtSettings Jwt { get; set; } = new();
    public EventBrokerSettings EventBroker { get; set; } = new();
    public ObservabilitySettings Observability { get; set; } = new();
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;
}

public class EventBrokerSettings
{
    public EventBrokerType BrokerType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}

public class ObservabilitySettings
{
    public string ServiceName { get; set; } = "bks.sdk.Service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string JaegerEndpoint { get; set; } = string.Empty;
}