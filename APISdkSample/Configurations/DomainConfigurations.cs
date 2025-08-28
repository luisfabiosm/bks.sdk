using bks.sdk.Transactions;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Models.Results;
using Domain.Processors;
using Domain.Services;

namespace Configurations
{
    public static class DomainConfigurations
    {
        public static IServiceCollection ConfigureDomainAdapters(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<DebitoProcessor>();
            services.AddScoped<ITransactionProcessor<DebitoResult>>(provider => provider.GetRequiredService<DebitoProcessor>());

            //Serviços de domínio
            services.AddScoped<ILimiteService, LimiteService>();
            services.AddScoped<IFraudeService, FraudeService>();


            return services;
        }
    }
}
