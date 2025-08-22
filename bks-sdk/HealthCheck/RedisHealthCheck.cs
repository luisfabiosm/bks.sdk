using bks.sdk.Cache;
using bks.sdk.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.HealthCheck
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly SDKSettings _settings;

        public RedisHealthCheck(ICacheProvider cacheProvider, SDKSettings settings)
        {
            _cacheProvider = cacheProvider;
            _settings = settings;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var testKey = $"health_check_{Guid.NewGuid():N}";
                var testValue = DateTime.UtcNow.ToString("O");

                // Testar operações básicas do Redis
                await _cacheProvider.SetAsync(testKey, testValue, TimeSpan.FromSeconds(30));
                var retrievedValue = await _cacheProvider.GetAsync(testKey);

                var data = new Dictionary<string, object>
                {
                    ["connection_string"] = MaskConnectionString(_settings.Redis.ConnectionString),
                    ["instance_name"] = _settings.Redis.InstanceName,
                    ["test_successful"] = retrievedValue == testValue
                };

                if (retrievedValue != testValue)
                {
                    return HealthCheckResult.Unhealthy(
                        "Redis cache operation failed", data: data);
                }

                return HealthCheckResult.Healthy(
                    "Redis cache is working properly", data: data);
            }
            catch (Exception ex)
            {
                var data = new Dictionary<string, object>
                {
                    ["connection_string"] = MaskConnectionString(_settings.Redis.ConnectionString),
                    ["error"] = ex.Message
                };

                return HealthCheckResult.Unhealthy(
                    "Redis cache is not available", ex, data);
            }
        }

        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "Not configured";

            // Mascarar informações sensíveis
            var parts = connectionString.Split(',');
            return parts.Length > 0 ? $"{parts[0]}:****" : "****";
        }
    }
}
