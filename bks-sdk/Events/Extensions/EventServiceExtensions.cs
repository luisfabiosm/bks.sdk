using bks.sdk.Common.Enums;
using bks.sdk.Core.Configuration;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Providers.InMemory;
using bks.sdk.Events.Providers.Kafka;
using bks.sdk.Events.Providers.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Extensions;

public static class EventServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkEvents(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new BKSFrameworkSettings();
        configuration.GetSection("BKSFramework").Bind(settings);

        if (!settings.Events.Enabled)
        {
            // Se eventos estão desabilitados, registrar implementação no-op
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
            services.AddSingleton<IEventSubscriber, NoOpEventSubscriber>();
            return services;
        }

        // Registrar baseado no provider configurado
        switch (settings.Events.Provider)
        {
            case EventProvider.InMemory:
                services.AddSingleton<InMemoryEventDispatcher>();
                services.AddSingleton<InMemoryEventPublisher>();
                services.AddSingleton<IEventPublisher>(provider => provider.GetRequiredService<InMemoryEventPublisher>());
                services.AddSingleton<IEventSubscriber>(provider => provider.GetRequiredService<InMemoryEventPublisher>());
                break;

            case EventProvider.RabbitMQ:
                ValidateRabbitMQConnection(settings);
                services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
                services.AddSingleton<IEventSubscriber, RabbitMQEventSubscriber>();
                break;

            case EventProvider.Kafka:
                ValidateKafkaConnection(settings);
                services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
                services.AddSingleton<IEventSubscriber, KafkaEventSubscriber>();
                break;

            default:
                throw new InvalidOperationException($"Provider de eventos não suportado: {settings.Events.Provider}");
        }

        // Registrar handlers automaticamente
        RegisterEventHandlers(services);

        return services;
    }

    private static void ValidateRabbitMQConnection(BKSFrameworkSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Events.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString é obrigatório para RabbitMQ");
        }

        if (!settings.Events.ConnectionString.StartsWith("amqp://") &&
            !settings.Events.ConnectionString.StartsWith("amqps://"))
        {
            throw new InvalidOperationException("ConnectionString inválido para RabbitMQ. Use formato: amqp://user:pass@host:port/");
        }
    }

    private static void ValidateKafkaConnection(BKSFrameworkSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Events.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString (BootstrapServers) é obrigatório para Kafka");
        }
    }

    private static void RegisterEventHandlers(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, handlerType);
                }
            }
        }
    }
}

