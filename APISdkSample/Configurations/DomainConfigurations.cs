using bks.sdk.Transactions;
using Domain.Core.Interfaces.Domain;
using Domain.Processors;
using Domain.Services;

namespace Configurations
{
    public static class DomainConfigurations
    {
        public static IServiceCollection ConfigureDomainAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            // Registro dos processadores de transação
            services.AddScoped<DebitoProcessor>();

            //Serviços de domínio
            services.AddScoped<ILimiteService, LimiteService>();
            services.AddScoped<IFraudeService, FraudeService>();


            return services;
        }
    }
}
