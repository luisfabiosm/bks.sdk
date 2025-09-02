

namespace bks.sdk.Middlewares.Extensions;

public class BKSFrameworkMiddlewareOptions
{
    public bool EnableRateLimiting { get; set; } = false;
    public bool EnableGlobalExceptionHandling { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;
}
