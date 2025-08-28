using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Middlewares
{
    public static class SDKMiddlewareExtensions
    {

        public static WebApplication UseBKSSDK(this WebApplication app)
        {

            // 1. Middleware de correlação de transações (deve vir primeiro)
            app.UseMiddleware<TransactionCorrelationMiddleware>();

            // 2. Middleware de métricas HTTP (instrumentação completa)
            app.UseHttpMetrics();

            // 3. Middleware de métricas de negócio
            //app.UseBusinessMetrics();

            // 4. Middleware de logging de requests (após métricas)
            app.UseMiddleware<RequestLoggingMiddleware>();


            // Autenticação e autorização
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
