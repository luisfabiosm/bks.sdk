using bks.sdk.Core.Configuration;
using bks.sdk.Core.Pipeline;
using bks.sdk.Core.Pipeline.Steps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Extensions;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registrar configurações
        var settings = new BKSFrameworkSettings();
        configuration.GetSection("BKSFramework").Bind(settings);
        services.AddSingleton(settings);

        // Validar configurações obrigatórias
        ValidateSettings(settings);

        // Registrar pipeline
        services.AddScoped<IPipelineExecutor, PipelineExecutor>();

        // Registrar etapas da pipeline
        services.AddScoped(typeof(ValidationStep<,>));
        services.AddScoped(typeof(PreProcessingStep<,>));
        services.AddScoped(typeof(ProcessingStep<,>));
        services.AddScoped(typeof(PostProcessingStep<,>));

        return services;
    }

    private static void ValidateSettings(BKSFrameworkSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.LicenseKey))
            errors.Add("LicenseKey é obrigatório");

        if (string.IsNullOrWhiteSpace(settings.ApplicationName))
            errors.Add("ApplicationName é obrigatório");

        if (string.IsNullOrWhiteSpace(settings.Security.Jwt.SecretKey))
            errors.Add("Security.Jwt.SecretKey é obrigatório");

        if (settings.Events.Enabled && string.IsNullOrWhiteSpace(settings.Events.ConnectionString))
            errors.Add("Events.ConnectionString é obrigatório quando Events.Enabled = true");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Configuração inválida do BKS Framework: {string.Join(", ", errors)}");
        }
    }
}

