using bks.sdk.Common.Enums;
using bks.sdk.Core.Initialization;
using bks.sdk.Processing.Abstractions;
using bks.sdk.Processing.Mediator;
using bks.sdk.Processing.Mediator.Abstractions;
using bks.sdk.Processing.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Extensions;
public static class ProcessingServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkProcessing(
        this IServiceCollection services,
        BKSFrameworkOptions options)
    {
        if (options.ProcessingMode == ProcessingMode.Mediator)
        {
            services.AddBKSFrameworkMediator();
        }
        else
        {
            services.AddBKSFrameworkTransactionProcessor();
        }

        return services;
    }

    public static IServiceCollection AddBKSFrameworkMediator(this IServiceCollection services)
    {
        // Registrar mediator
        services.AddScoped<IBKSMediator, Mediator.BKSMediator>();

        // Registrar processador do mediator
        services.AddScoped(typeof(IBKSMediatorProcessor<,>), typeof(BKSMediatorProcessor<,>));

        // Registrar handlers automaticamente
        RegisterMediatorHandlers(services);

        return services;
    }

    public static IServiceCollection AddBKSFrameworkTransactionProcessor(this IServiceCollection services)
    {
        // Registrar processador de transações
        services.AddScoped(typeof(IBKSTransactionProcessor<,>), typeof(TransactionProcessor<,>));

        // Registrar processadores automaticamente
        RegisterTransactionProcessors(services);

        return services;
    }

    private static void RegisterMediatorHandlers(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IBKSRequestHandler<,>)))
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IBKSRequestHandler<,>));

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, handlerType);
                }
            }
        }
    }

    private static void RegisterTransactionProcessors(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var processorTypes = assembly.GetTypes()
                .Where(t => t.BaseType != null &&
                           t.BaseType.IsGenericType &&
                           t.BaseType.GetGenericTypeDefinition() == typeof(Transactions.Processors.BaseTransactionProcessor<,>))
                .Where(t => !t.IsAbstract);

            foreach (var processorType in processorTypes)
            {
                // Registrar o processador específico
                services.AddScoped(processorType);

                // Registrar também pela interface genérica
                var baseType = processorType.BaseType!;
                var genericArgs = baseType.GetGenericArguments();
                var interfaceType = typeof(IBKSTransactionProcessor<,>).MakeGenericType(genericArgs);

                services.AddScoped(interfaceType, processorType);
            }
        }
    }
}

