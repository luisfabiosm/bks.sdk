using bks.sdk.Core.Configuration;
using bks.sdk.Observability.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace bks.sdk.Observability
{
    public static class ObservabilityInitializer
    {
        public static IServiceCollection AddBKSObservability(
            this IServiceCollection services,
            ObservabilitySettings observabilitySettings)
        {
            // Configurar ResourceBuilder
            var resourceBuilder = CreateResourceBuilder(observabilitySettings);

            // Configurar OpenTelemetry
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource = resourceBuilder)
                .WithTracing(tracing => ConfigureTracing(tracing, observabilitySettings, resourceBuilder))
                .WithMetrics(metrics => ConfigureMetrics(metrics, observabilitySettings, resourceBuilder));

            // Registrar serviços customizados
            services.AddSingleton<IBKSTracer, OpenTelemetryBKSTracer>();

            return services;
        }

        private static ResourceBuilder CreateResourceBuilder(ObservabilitySettings settings)
        {
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: settings.ServiceName,
                    serviceVersion: settings.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["bks.sdk.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                });

            // Adicionar atributos customizados
            if (settings.OpenTelemetry.ResourceAttributes.Any())
            {
                resourceBuilder.AddAttributes(
                    settings.OpenTelemetry.ResourceAttributes.Select(kv =>
                        new KeyValuePair<string, object>(kv.Key, kv.Value)));
            }

            return resourceBuilder;
        }

        private static void ConfigureTracing(
            TracerProviderBuilder tracing,
            ObservabilitySettings settings,
            ResourceBuilder resourceBuilder)
        {
            tracing
                .SetResourceBuilder(resourceBuilder)
                .SetSampler(new TraceIdRatioBasedSampler(settings.OpenTelemetry.TracingSampleRate))
                .AddSource("bks.sdk") // Source name para spans customizados
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                    {
                        // Filtrar health checks e outros endpoints internos
                        var path = httpContext.Request.Path.Value?.ToLower();
                        return !path?.Contains("/health") == true &&
                               !path?.Contains("/metrics") == true;
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                });
                //.AddEntityFrameworkCoreInstrumentation(options =>
                //{
                //    options.SetDbStatementForText = true;
                //})
                //.AddRedisInstrumentation()
                //.AddSqlClientInstrumentation(options =>
                //{
                //    options.SetDbStatementForText = true;
                //    options.RecordException = true;
                //}
                //);

            // Configurar exportadores
            ConfigureTracingExporters(tracing, settings);
        }

        private static void ConfigureTracingExporters(
            TracerProviderBuilder tracing,
            ObservabilitySettings settings)
        {
            var exporters = settings.OpenTelemetry.Exporters;

            // OTLP Exporter (prioritário)
            if (!string.IsNullOrWhiteSpace(settings.OpenTelemetry.OtlpEndpoint) ||
                !string.IsNullOrWhiteSpace(exporters.Otlp.Endpoint))
            {
                tracing.AddOtlpExporter(options =>
                {
                    var endpoint = !string.IsNullOrWhiteSpace(settings.OpenTelemetry.OtlpEndpoint)
                        ? settings.OpenTelemetry.OtlpEndpoint
                        : exporters.Otlp.Endpoint;

                    // Permitir override via variável de ambiente
                    endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? endpoint;

                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        options.Endpoint = new Uri(endpoint);
                    }

                    options.Protocol = exporters.Otlp.Protocol.ToLower() switch
                    {
                        "http" => OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf,
                        "grpc" => OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
                        _ => OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                    };

                    options.TimeoutMilliseconds = exporters.Otlp.TimeoutMilliseconds;

                    // Adicionar headers customizados
                    if (exporters.Otlp.Headers.Any())
                    {
                        options.Headers = string.Join(",",
                            exporters.Otlp.Headers.Select(h => $"{h.Key}={h.Value}"));
                    }
                });
            }
            //// Jaeger (compatibilidade)
            //else if (settings.OpenTelemetry.EnableJaegerExporter ||
            //         !string.IsNullOrWhiteSpace(settings.JaegerEndpoint))
            //{
            //    tracing.AddJaegerExporter(options =>
            //    {
            //        var jaegerEndpoint = !string.IsNullOrWhiteSpace(settings.JaegerEndpoint)
            //            ? settings.JaegerEndpoint
            //            : exporters.Jaeger.Endpoint;

            //        if (!string.IsNullOrWhiteSpace(jaegerEndpoint))
            //        {
            //            options.Endpoint = new Uri(jaegerEndpoint);
            //        }
            //        else
            //        {
            //            options.AgentHost = exporters.Jaeger.AgentHost;
            //            options.AgentPort = exporters.Jaeger.AgentPort;
            //        }
            //    });
            //}

            // Console Exporter (desenvolvimento)
            if (settings.OpenTelemetry.EnableConsoleExporter || exporters.EnableConsole)
            {
                tracing.AddConsoleExporter();
            }
        }

        private static void ConfigureMetrics(
            MeterProviderBuilder metrics,
            ObservabilitySettings settings,
            ResourceBuilder resourceBuilder)
        {
            metrics
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("bks.sdk") // Meter name para métricas customizadas
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation();

            // Configurar exportadores de métricas
            ConfigureMetricsExporters(metrics, settings);
        }

        private static void ConfigureMetricsExporters(
            MeterProviderBuilder metrics,
            ObservabilitySettings settings)
        {
            var exporters = settings.OpenTelemetry.Exporters;

            // OTLP Exporter para métricas
            if (!string.IsNullOrWhiteSpace(settings.OpenTelemetry.OtlpEndpoint) ||
                !string.IsNullOrWhiteSpace(exporters.Otlp.Endpoint))
            {
                metrics.AddOtlpExporter(options =>
                {
                    var endpoint = !string.IsNullOrWhiteSpace(settings.OpenTelemetry.OtlpEndpoint)
                        ? settings.OpenTelemetry.OtlpEndpoint
                        : exporters.Otlp.Endpoint;

                    endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? endpoint;

                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        options.Endpoint = new Uri(endpoint);
                    }

                    options.Protocol = exporters.Otlp.Protocol.ToLower() switch
                    {
                        "http" => OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf,
                        "grpc" => OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
                        _ => OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                    };
                });
            }

            // Console Exporter para métricas (desenvolvimento)
            if (settings.OpenTelemetry.EnableConsoleExporter || exporters.EnableConsole)
            {
                metrics.AddConsoleExporter();
            }
        }
    }
}