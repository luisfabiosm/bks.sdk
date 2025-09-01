using bks.sdk.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;

public record BKSFrameworkSettings
{
    public string LicenseKey { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public ProcessingConfiguration Processing { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
    public EventsConfiguration Events { get; set; } = new();
    public ObservabilityConfiguration Observability { get; set; } = new();
}

public record DataEncryptionConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Algorithm { get; set; } = "AES256";
    public string? KeyVaultUrl { get; set; }
}

public record EventsConfiguration
{
    public bool Enabled { get; set; } = false;
    public EventProvider Provider { get; set; } = EventProvider.InMemory;
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicPrefix { get; set; } = "bks-framework";
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}

public record JwtConfiguration
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public record LoggingConfiguration
{
    public string Level { get; set; } = "Information";
    public bool IncludeScopes { get; set; } = true;
    public bool WriteToConsole { get; set; } = true;
    public bool WriteToFile { get; set; } = true;
    public string FilePath { get; set; } = "logs/bks-framework-.log";
}

public record ObservabilityConfiguration
{
    public string ServiceName { get; set; } = "bks-framework-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public LoggingConfiguration Logging { get; set; } = new();
    public TracingConfiguration Tracing { get; set; } = new();
}

public record ProcessingConfiguration
{
    public ProcessingMode Mode { get; set; } = ProcessingMode.Mediator;
    public bool EnablePipelineEvents { get; set; } = true;
    public bool ValidationEnabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

public record TracingConfiguration
{
    public double SamplingRate { get; set; } = 1.0;
    public string OtlpEndpoint { get; set; } = string.Empty;
    public bool EnableConsoleExporter { get; set; } = false;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
}

public record SecurityConfiguration
{
    public JwtConfiguration Jwt { get; set; } = new();
    public DataEncryptionConfiguration DataEncryption { get; set; } = new();
}
