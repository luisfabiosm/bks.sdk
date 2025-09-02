using bks.sdk.Core.Configuration;
using bks.sdk.Observability.Correlation;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Performance;
using bks.sdk.Observability.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Extensions;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new BKSFrameworkSettings();
        configuration.GetSection("BKSFramework").Bind(settings);

        // Configurar Serilog
        ConfigureSerilog(services, settings);

        // Configurar OpenTelemetry
        ConfigureOpenTelemetry(services, settings);

        // Registrar serviços de observabilidade
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddSingleton<IPerformanceTracker, PerformanceTracker>();

        return services;
    }

    private static void ConfigureSerilog(IServiceCollection services, BKSFrameworkSettings settings)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(GetSerilogLevel(settings.Observability.Logging.Level))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", settings.ApplicationName)
            .Enrich.WithProperty("ServiceName", settings.Observability.ServiceName)
            .Enrich.WithProperty("ServiceVersion", settings.Observability.ServiceVersion)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId);

        // Console sink
        if (settings.Observability.Logging.WriteToConsole)
        {
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // File sink
        if (settings.Observability.Logging.WriteToFile)
        {
            var filePath = settings.Observability.Logging.FilePath.Replace("{ApplicationName}", settings.ApplicationName);
            loggerConfig = loggerConfig.WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        Log.Logger = loggerConfig.CreateLogger();

        // Registrar Serilog com Microsoft.Extensions.Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Registrar IBKSLogger
        services.AddSingleton<IBKSLogger>(provider =>
        {
            var msLogger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SerilogBKSLogger>>();
            return new SerilogBKSLogger(Log.Logger, msLogger);
        });
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services, BKSFrameworkSettings settings)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: settings.Observability.ServiceName,
                serviceVersion: settings.Observability.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["bks.framework.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                ["machine.name"] = Environment.MachineName,
                ["process.id"] = Environment.ProcessId.ToString()
            });

        // Adicionar atributos customizados
        if (settings.Observability.Tracing.ResourceAttributes.Any())
        {
            resourceBuilder.AddAttributes(
                settings.Observability.Tracing.ResourceAttributes.Select(kv =>
                    new KeyValuePair<string, object>(kv.Key, kv.Value)));
        }

        services.AddOpenTelemetry()
            //.ConfigureResource(resource => resource.Merge(resourceBuilder))
            .ConfigureResource(resource => resource
            .AddService(
                serviceName: settings.Observability.ServiceName,
                serviceVersion: settings.Observability.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["bks.framework.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                ["machine.name"] = Environment.MachineName,
                ["process.id"] = Environment.ProcessId.ToString()
            })
            // Adicionar atributos customizados
            .AddAttributes(
                settings.Observability.Tracing.ResourceAttributes.Select(kv =>
                    new KeyValuePair<string, object>(kv.Key, kv.Value)))
            )
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(settings.Observability.Tracing.SamplingRate))
                    .AddSource("bks.sdk")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLower();
                            return !path?.Contains("/health") == true;
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                // Configurar exportadores
                ConfigureTracingExporters(tracing, settings);
            });

        // Registrar tracer customizado
        services.AddSingleton<IBKSTracer, OpenTelemetryBKSTracer>();
    }

    private static void ConfigureTracingExporters(TracerProviderBuilder tracing, BKSFrameworkSettings settings)
    {
        // OTLP Exporter
        if (!string.IsNullOrWhiteSpace(settings.Observability.Tracing.OtlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(settings.Observability.Tracing.OtlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        }

        // Console Exporter (desenvolvimento)
        if (settings.Observability.Tracing.EnableConsoleExporter)
        {
            tracing.AddConsoleExporter();
        }
    }

    private static LogEventLevel GetSerilogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "trace" or "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}


