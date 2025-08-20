using bks.sdk.Transactions;
using Domain.UseCases;

namespace Configurations
{
    public static class DomainConfigurations
    {
        public static IServiceCollection ConfigureDomainAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            // Registro dos processadores de transação
            builder.Services.AddScoped<ITransactionProcessor, EntryDebitProcessor>();

            return services;
        }
    }
}
