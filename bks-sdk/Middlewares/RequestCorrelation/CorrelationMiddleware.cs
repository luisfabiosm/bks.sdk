using bks.sdk.Observability.Correlation;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.RequestCorrelation;


public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationMiddleware(RequestDelegate next, ICorrelationContextAccessor correlationContextAccessor)
    {
        _next = next;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Este middleware é simplificado pois a lógica principal está no BKSFrameworkMiddleware
        // Pode ser usado independentemente se necessário

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString("N");

        _correlationContextAccessor.CorrelationId = correlationId;
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        await _next(context);
    }
}
