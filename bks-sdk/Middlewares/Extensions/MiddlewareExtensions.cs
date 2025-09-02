using bks.sdk.Middlewares.ExceptionHandling;
using bks.sdk.Middlewares.RequestCorrelation;
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

        // Exception Handling Global (capturar todas as exceções)
        app.UseMiddleware<GlobalExceptionMiddleware>();


        // BKS Framework Middleware (middleware principal unificado)
        app.UseMiddleware<BKSFrameworkMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseBKSFrameworkMiddleware(
        this IApplicationBuilder app,
        Action<BKSFrameworkMiddlewareOptions> configure)
    {
        var options = new BKSFrameworkMiddlewareOptions();
        configure(options);

        if (options.EnableGlobalExceptionHandling)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
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

}

