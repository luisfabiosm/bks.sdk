using bks.sdk.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public static class TransactionHandlerExtensions
    {

        public static IServiceCollection AddTransactionHandlers(this IServiceCollection services, Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => IsTransactionHandler(t))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceTypes = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(ITransactionHandler<,>))
                    .ToList();

                foreach (var interfaceType in interfaceTypes)
                {
                    services.AddScoped(interfaceType, handlerType);
                }

                services.AddScoped(handlerType);
            }

            return services;
        }


        public static IServiceCollection AddTransactionValidators(this IServiceCollection services, Assembly assembly)
        {
            var validatorTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => IsTransactionValidator(t))
                .ToList();

            foreach (var validatorType in validatorTypes)
            {
                var interfaceTypes = validatorType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(ITransactionValidator<>))
                    .ToList();

                foreach (var interfaceType in interfaceTypes)
                {
                    services.AddScoped(interfaceType, validatorType);
                }
            }

            return services;
        }

        private static bool IsTransactionHandler(Type type)
        {
            return type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(ITransactionHandler<,>));
        }

        private static bool IsTransactionValidator(Type type)
        {
            return type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(ITransactionValidator<>));
        }
    }

}
