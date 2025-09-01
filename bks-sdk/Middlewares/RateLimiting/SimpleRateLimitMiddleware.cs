using bks.sdk.Observability.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.RateLimiting;

public class SimpleRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly IBKSLogger _logger;
    private readonly ConcurrentDictionary<string, ClientRequestInfo> _clients;

    public SimpleRateLimitMiddleware(
        RequestDelegate next,
        RateLimitOptions options,
        IBKSLogger logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
        _clients = new ConcurrentDictionary<string, ClientRequestInfo>();

        // Limpeza periódica de clientes inativos
        _ = Task.Run(CleanupExpiredClients);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        var clientInfo = _clients.AddOrUpdate(clientId,
            new ClientRequestInfo { FirstRequest = now, RequestCount = 1, LastRequest = now },
            (key, existing) =>
            {
                // Reset contador se passou o período da janela
                if (now - existing.FirstRequest > _options.Window)
                {
                    return new ClientRequestInfo { FirstRequest = now, RequestCount = 1, LastRequest = now };
                }

                existing.RequestCount++;
                existing.LastRequest = now;
                return existing;
            });

        // Verificar se excedeu o limite
        if (clientInfo.RequestCount > _options.MaxRequests)
        {
            _logger.Warn($"Rate limit exceeded for client {clientId}: {clientInfo.RequestCount} requests in {_options.Window}");

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Append("Retry-After", _options.Window.TotalSeconds.ToString());
            context.Response.Headers.Append("X-RateLimit-Limit", _options.MaxRequests.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", "0");
            context.Response.Headers.Append("X-RateLimit-Reset",
                ((DateTimeOffset)(clientInfo.FirstRequest.Add(_options.Window))).ToUnixTimeSeconds().ToString());

            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // Adicionar headers informativos
        var remaining = Math.Max(0, _options.MaxRequests - clientInfo.RequestCount);
        context.Response.Headers.Append("X-RateLimit-Limit", _options.MaxRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset",
            ((DateTimeOffset)(clientInfo.FirstRequest.Add(_options.Window))).ToUnixTimeSeconds().ToString());

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Primeiro tentar por usuário autenticado
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value
                      ?? context.User.FindFirst("id")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
                return $"user:{userId}";
        }

        // Fallback para IP
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return $"ip:{xForwardedFor.Split(',')[0].Trim()}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
    }

    private async Task CleanupExpiredClients()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5)); // Cleanup a cada 5 minutos

                var now = DateTime.UtcNow;
                var expiredClients = _clients
                    .Where(kvp => now - kvp.Value.LastRequest > _options.Window.Add(TimeSpan.FromMinutes(10)))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var clientId in expiredClients)
                {
                    _clients.TryRemove(clientId, out _);
                }

                if (expiredClients.Count > 0)
                {
                    _logger.Debug($"Cleaned up {expiredClients.Count} expired rate limit entries");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during rate limit cleanup");
            }
        }
    }

    private class ClientRequestInfo
    {
        public DateTime FirstRequest { get; set; }
        public int RequestCount { get; set; }
        public DateTime LastRequest { get; set; }
    }
}

