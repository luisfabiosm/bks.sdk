
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

            // Health Checks
            monitoringGroup.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(x => new
                        {
                            name = x.Key,
                            status = x.Value.Status.ToString(),
                            duration = x.Value.Duration.TotalMilliseconds
                        }),
                        totalDuration = report.TotalDuration.TotalMilliseconds
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            });

        }
    }
}
