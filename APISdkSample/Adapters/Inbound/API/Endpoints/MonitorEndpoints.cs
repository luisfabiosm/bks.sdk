
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Adapters.Inbound.API.Endpoints
{
    public static partial class MonitorEndpoints
    {

        public static void AddMonitorEndpoints(this WebApplication app)
        {
            var monitoringGroup = app.MapGroup("api/sdk/sample")
                               .WithTags("Monitoramento")
                               .AllowAnonymous();

            // Health Check geral
            monitoringGroup.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            })
            .WithName("HealthCheck")
            .WithSummary("Verificação geral de saúde do sistema")
            .WithDescription("Retorna o status de saúde de todos os componentes do sistema");

            // Health Check específico por tag
            monitoringGroup.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            })
            .WithName("ReadinessCheck")
            .WithSummary("Verificação de prontidão do sistema");

            monitoringGroup.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("sdk"),
                ResponseWriter = WriteHealthCheckResponse
            })
            .WithName("LivenessCheck")
            .WithSummary("Verificação de vitalidade do sistema");

            // Health Check de dependências externas
            monitoringGroup.MapHealthChecks("/health/external", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("external"),
                ResponseWriter = WriteHealthCheckResponse
            })
            .WithName("ExternalDependenciesCheck")
            .WithSummary("Verificação de dependências externas");

            // Endpoint de informações do SDK
            monitoringGroup.MapGet("/info", GetSDKInfo)
                .WithName("SDKInfo")
                .WithSummary("Informações do BKS SDK")
                .WithDescription("Retorna informações detalhadas sobre a versão e configuração do SDK");

        }

        private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    duration = x.Value.Duration.TotalMilliseconds,
                    description = x.Value.Description,
                    data = x.Value.Data.Any() ? x.Value.Data : null,
                    exception = x.Value.Exception?.Message,
                    tags = x.Value.Tags
                }).ToArray()
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }

        private static IResult GetSDKInfo()
        {
            var sdkInfo = new
            {
                name = "BKS SDK",
                version = "1.0.3",
                description = "Framework robusto para processamento de transações financeiras",
                build = new
                {
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    framework = Environment.Version.ToString(),
                    osVersion = Environment.OSVersion.ToString(),
                    machineName = Environment.MachineName
                },
                features = new[]
                {
                    "Transaction Processing",
                    "JWT Authentication",
                    "Event Broker",
                    "Distributed Caching",
                    "OpenTelemetry Observability",
                    "Health Checks"
                },
                endpoints = new
                {
                    health = "/api/sdk/sample/health",
                    readiness = "/api/sdk/sample/health/ready",
                    liveness = "/api/sdk/sample/health/live",
                    external = "/api/sdk/sample/health/external",
                    info = "/api/sdk/sample/info"
                }
            };

            return Results.Ok(sdkInfo);
        }
    }
}
