using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Helpers;

public static class ConfigurationHelper
{
    public static T GetRequiredValue<T>(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (value == null)
        {
            throw new InvalidOperationException($"Configuration key '{key}' is required but was not found");
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Configuration key '{key}' could not be converted to type {typeof(T).Name}", ex);
        }
    }

    public static T GetValueOrDefault<T>(this IConfiguration configuration, string key, T defaultValue = default!)
    {
        var value = configuration[key];
        if (value == null)
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public static string GetConnectionString(this IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is required but was not found");
        }

        return connectionString;
    }

    public static bool IsProduction(this IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["Environment"];
        return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDevelopment(this IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["Environment"];
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}

