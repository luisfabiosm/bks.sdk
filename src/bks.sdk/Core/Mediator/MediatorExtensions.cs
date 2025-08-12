using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace bks.sdk.Core.Mediator
{
    public static class MediatorExtensions
    {

        public static IServiceCollection AddBksMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddScoped<IBksMediator, BksMediator>();

            var assembliesToScan = assemblies.Length > 0 ? assemblies : new[] { Assembly.GetCallingAssembly() };

            foreach (var assembly in assembliesToScan)
            {
                RegisterHandlers(services, assembly);
            }

            return services;
        }


        public static IServiceCollection AddBksMediatorWithAutoScan(this IServiceCollection services)
        {
            services.AddScoped<IBksMediator, BksMediator>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToArray();

            foreach (var assembly in assemblies)
            {
                RegisterHandlers(services, assembly);
            }

            return services;
        }

        private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ITransactionHandler<,>)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(ITransactionHandler<,>));

                services.AddScoped(interfaceType, handlerType);
                services.AddScoped(handlerType);
            }
        }
    }


}
