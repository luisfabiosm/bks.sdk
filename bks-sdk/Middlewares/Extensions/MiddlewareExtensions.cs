using bks.sdk.Middlewares.ExceptionHandling;
using bks.sdk.Middlewares.RateLimiting;
using bks.sdk.Middlewares.RequestCorrelation;
using bks.sdk.Middlewares.Security;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseBKSFrameworkMiddleware(this IApplicationBuilder app)
    {
        // Ordem dos middlewares é importante!

        // 1. Rate Limiting (primeiro para bloquear excesso de requisições rapidamente)
        app.UseMiddleware<SimpleRateLimitMiddleware>();

        // 2. Exception Handling Global (capturar todas as exceções)
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 3. Security Headers (adicionar headers de segurança)
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // 4. BKS Framework Middleware (middleware principal unificado)
        app.UseMiddleware<BKSFrameworkMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseBKSFrameworkMiddleware(
        this IApplicationBuilder app,
        Action<BKSFrameworkMiddlewareOptions> configure)
    {
        var options = new BKSFrameworkMiddlewareOptions();
        configure(options);

        if (options.EnableRateLimiting)
        {
            app.UseMiddleware<SimpleRateLimitMiddleware>(options.RateLimitOptions);
        }

        if (options.EnableGlobalExceptionHandling)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
        }

        if (options.EnableSecurityHeaders)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>(options.SecurityHeadersOptions);
        }

        // BKS Framework Middleware sempre habilitado
        app.UseMiddleware<BKSFrameworkMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SecurityHeadersOptions? options = null)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }

    public static IApplicationBuilder UseRateLimit(this IApplicationBuilder app, RateLimitOptions options)
    {
        return app.UseMiddleware<SimpleRateLimitMiddleware>(options);
    }
}

