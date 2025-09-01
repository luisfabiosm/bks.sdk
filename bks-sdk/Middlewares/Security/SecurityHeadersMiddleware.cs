using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions? options = null)
    {
        _next = next;
        _options = options ?? new SecurityHeadersOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Adicionar headers de segurança na resposta
        AddSecurityHeaders(context.Response);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpResponse response)
    {
        // Prevent MIME type sniffing
        if (_options.AddXContentTypeOptions)
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }

        // Prevent clickjacking
        if (_options.AddXFrameOptions)
        {
            response.Headers.Append("X-Frame-Options", _options.XFrameOptionsValue);
        }

        // XSS Protection
        if (_options.AddXXSSProtection)
        {
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
        }

        // Strict Transport Security
        if (_options.AddHSTS && !string.IsNullOrWhiteSpace(_options.HSTSValue))
        {
            response.Headers.Append("Strict-Transport-Security", _options.HSTSValue);
        }

        // Content Security Policy
        if (_options.AddCSP && !string.IsNullOrWhiteSpace(_options.CSPValue))
        {
            response.Headers.Append("Content-Security-Policy", _options.CSPValue);
        }

        // Referrer Policy
        if (_options.AddReferrerPolicy)
        {
            response.Headers.Append("Referrer-Policy", _options.ReferrerPolicyValue);
        }

        // Feature Policy / Permissions Policy
        if (_options.AddPermissionsPolicy && !string.IsNullOrWhiteSpace(_options.PermissionsPolicyValue))
        {
            response.Headers.Append("Permissions-Policy", _options.PermissionsPolicyValue);
        }
    }
}
