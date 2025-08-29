﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Initialization;

public static class BKSFrameworkInitializer
{
    public static IServiceCollection AddBKSFramework(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<BKSFrameworkOptions>? configure = null)
    {
        var options = new BKSFrameworkOptions();
        configure?.Invoke(options);

        // 1. Core - Configurações e Pipeline
        services.AddBKSFrameworkCore(configuration);

        // 2. Security - Autenticação e Criptografia
        services.AddBKSFrameworkSecurity(configuration);

        // 3. Validation - Sistema de validação interno
        services.AddBKSFrameworkValidation();

        // 4. Processing - Mediator ou TransactionProcessor
        services.AddBKSFrameworkProcessing(options);

        // 5. Events - Sistema de eventos (se habilitado)
        services.AddBKSFrameworkEvents(configuration);

        // 6. Observability - Logging e Tracing
        services.AddBKSFrameworkObservability(configuration);

        return services;
    }

    public static WebApplication UseBKSFramework(this WebApplication app)
    {
        // Middleware unificado
        app.UseBKSFrameworkMiddleware();

        // Autenticação e autorização
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}

