using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Middlewares
{
    public static class HttpMetricsMiddlewareExtensions
    {
        /// <summary>
        /// Adiciona o middleware de métricas HTTP ao pipeline
        /// </summary>
        public static IApplicationBuilder UseHttpMetrics(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpMetricsMiddleware>();
        }
    }
}
