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
            // Middleware de correlação de transações (deve vir primeiro)
            app.UseMiddleware<TransactionCorrelationMiddleware>();

            // Middleware de logging de requests
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Autenticação e autorização
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
