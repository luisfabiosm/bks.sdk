using bks.sdk.Middlewares.RateLimiting;
using bks.sdk.Middlewares.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.Extensions;

public class BKSFrameworkMiddlewareOptions
{
    public bool EnableRateLimiting { get; set; } = false;
    public bool EnableGlobalExceptionHandling { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;

    public RateLimitOptions RateLimitOptions { get; set; } = new();
    public SecurityHeadersOptions SecurityHeadersOptions { get; set; } = new();
}
