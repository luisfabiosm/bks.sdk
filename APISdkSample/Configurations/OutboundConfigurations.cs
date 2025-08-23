using Adapters.Outbound.DataAdapter;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Interfaces.Outbound;

namespace Configurations
{
    public static class OutboundConfigurations
    {

        public static IServiceCollection ConfigureOutboundAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<IContaRepository, InMemoryContaRepository>();
        


            return services;
        }
    }
}
