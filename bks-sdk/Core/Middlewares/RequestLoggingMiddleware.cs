using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bks.sdk.Observability.Logging.IBKSLogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next, bks.sdk.Observability.Logging.IBKSLogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

                _logger.Info($"Requisição concluída - CorrelationId: {correlationId}, " +
                            $"Status: {context.Response.StatusCode}, " +
                            $"Duração: {sw.ElapsedMilliseconds}ms");
            }
        }
    }

}
