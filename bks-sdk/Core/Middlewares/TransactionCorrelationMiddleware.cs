using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Middlewares
{
 
    public class TransactionCorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bks.sdk.Observability.Logging.ILogger _logger;

        public TransactionCorrelationMiddleware(RequestDelegate next, bks.sdk.Observability.Logging.ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Gerar ou extrair correlation ID
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                              ?? Guid.NewGuid().ToString("N");

            // Adicionar ao contexto
            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            _logger.Info($"Processando requisição {context.Request.Method} {context.Request.Path} - CorrelationId: {correlationId}");

            await _next(context);
        }
    }

}
