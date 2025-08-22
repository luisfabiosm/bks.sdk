using bks.sdk.Authentication;
using bks.sdk.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.HealthCheck
{
    public class JwtHealthCheck : IHealthCheck
    {
        private readonly IJwtTokenProvider _jwtTokenProvider;
        private readonly SDKSettings _settings;

        public JwtHealthCheck(IJwtTokenProvider jwtTokenProvider, SDKSettings settings)
        {
            _jwtTokenProvider = jwtTokenProvider;
            _settings = settings;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["issuer"] = _settings.Jwt.Issuer,
                    ["audience"] = _settings.Jwt.Audience,
                    ["expiration_minutes"] = _settings.Jwt.ExpirationInMinutes,
                    ["secret_key_configured"] = !string.IsNullOrWhiteSpace(_settings.Jwt.SecretKey)
                };

                // Verificar configurações essenciais
                if (string.IsNullOrWhiteSpace(_settings.Jwt.SecretKey))
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "JWT secret key not configured", data: data));
                }

                if (string.IsNullOrWhiteSpace(_settings.Jwt.Issuer))
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "JWT issuer not configured", data: data));
                }

                // Testar geração e validação de token
                var claims = new[] { new Claim("test", "health_check") };
                var token = _jwtTokenProvider.GenerateToken("health_check", claims);
                var principal = _jwtTokenProvider.ValidateToken(token);

                var tokenWorking = principal != null &&
                                 principal.HasClaim("test", "health_check");

                data["token_generation_test"] = tokenWorking;

                return Task.FromResult(tokenWorking
                    ? HealthCheckResult.Healthy("JWT configuration is working properly", data: data)
                    : HealthCheckResult.Unhealthy("JWT token generation/validation failed", data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "JWT health check failed", ex));
            }
        }
    }
}
