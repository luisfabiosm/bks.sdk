using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using bks.sdk.Core.Configuration;
using bks.sdk.Observability;

namespace bks.sdk.Core.Extensions
{
    public static class ObservabilityExtensions
    {
      
        public static IServiceCollection AddBKSOpenTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var observabilitySettings = new ObservabilitySettings();
            configuration.GetSection("bkssdk:Observability").Bind(observabilitySettings);

            return services.AddBKSObservability(observabilitySettings);
        }

    
        public static IServiceCollection AddBKSOpenTelemetry(
            this IServiceCollection services,
            Action<ObservabilitySettings> configure)
        {
            var settings = new ObservabilitySettings();
            configure(settings);

            return services.AddBKSObservability(settings);
        }

  
        public static IServiceCollection AddBKSOpenTelemetryWithJaegerFallback(
            this IServiceCollection services,
            IConfiguration configuration,
            string? otlpEndpoint = null)
        {
            var observabilitySettings = new ObservabilitySettings();
            configuration.GetSection("bkssdk:Observability").Bind(observabilitySettings);

            // Se OTLP endpoint não foi fornecido, usar Jaeger como fallback
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                observabilitySettings.OpenTelemetry.OtlpEndpoint = otlpEndpoint;
            }
            //else if (string.IsNullOrWhiteSpace(observabilitySettings.OpenTelemetry.OtlpEndpoint))
            //{
            //    observabilitySettings.OpenTelemetry.EnableJaegerExporter = true;
            //}

            return services.AddBKSObservability(observabilitySettings);
        }
    }
}