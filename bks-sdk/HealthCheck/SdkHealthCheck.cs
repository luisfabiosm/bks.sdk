using bks.sdk.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.HealthCheck
{
    public class SdkHealthCheck : IHealthCheck
    {
        private readonly SDKSettings _settings;

        public SdkHealthCheck(SDKSettings settings)
        {
            _settings = settings;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificações básicas do SDK
                var data = new Dictionary<string, object>
                {
                    ["sdk_version"] = "1.0.3",
                    ["application_name"] = _settings.ApplicationName,
                    ["license_configured"] = !string.IsNullOrWhiteSpace(_settings.LicenseKey),
                    ["service_name"] = _settings.Observability.ServiceName
                };

                // Verificar se configurações essenciais estão presentes
                if (string.IsNullOrWhiteSpace(_settings.LicenseKey))
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "SDK license key not configured", data: data));
                }

                if (string.IsNullOrWhiteSpace(_settings.ApplicationName))
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "Application name not configured", data: data));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    "BKS SDK is running properly", data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "BKS SDK health check failed", ex));
            }
        }
    }
}
