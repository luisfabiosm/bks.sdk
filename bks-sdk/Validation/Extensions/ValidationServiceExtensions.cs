using bks.sdk.Validation.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Extensions;

public static class ValidationServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkValidation(this IServiceCollection services)
    {
        // Registrar validadores automaticamente
        RegisterValidators(services);

        return services;
    }

    public static IServiceCollection AddValidator<TValidator, TModel>(this IServiceCollection services)
        where TValidator : class, IValidator<TModel>
    {
        services.AddScoped<IValidator<TModel>, TValidator>();
        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Length == 0
            ? new[] { Assembly.GetCallingAssembly() }
            : assemblies;

        RegisterValidators(services, assembliesToScan);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services, Assembly[]? assemblies = null)
    {
        assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var validatorTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IValidator<>)))
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var validatorType in validatorTypes)
            {
                var interfaces = validatorType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IValidator<>));

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, validatorType);
                }
            }
        }
    }
}
