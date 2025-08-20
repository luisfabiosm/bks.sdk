using bks.sdk.Transactions;
using Domain.Processors;

namespace Configurations
{
    public static class DomainConfigurations
    {
        public static IServiceCollection ConfigureDomainAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            // Registro dos processadores de transação
            services.AddScoped<ITransactionProcessor, TransferenciaProcessor>();

            return services;
        }
    }
}
