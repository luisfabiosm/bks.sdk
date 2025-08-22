using Adapters.Inbound.API.Endpoints;
using bks.sdk.Core.Initialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Adapters.Inbound.API.Extensions
{
    public static class WebApiExtensions
    {
        public static IServiceCollection ConfigureAPI(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();

            // Configuração de CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://app.bks.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            return services;
        }

        public static void UseAPIExtensions(this WebApplication app)
        {
            app.UseSwaggerExtensions();
            app.AddTransactionEndpoints();
            app.AddMonitorEndpoints();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHealthChecks("/health");
            app.Run();
        }
    }
}
