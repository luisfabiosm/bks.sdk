using bks.sdk.Common.Enums;
using bks.sdk.Core.Configuration;
using bks.sdk.Observability.Correlation;
using bks.sdk.Observability.Diagnostics;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Performance;
using bks.sdk.Observability.Tracing;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;


namespace bks.sdk.Middlewares;

public class BKSFrameworkMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;
    private readonly IBKSTracer _tracer;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IPerformanceTracker _performanceTracker;

    public BKSFrameworkMiddleware(
        RequestDelegate next,
        BKSFrameworkSettings settings,
        IBKSLogger logger,
        IBKSTracer tracer,
        ICorrelationContextAccessor correlationContextAccessor,
        IPerformanceTracker performanceTracker)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
        _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware para certos paths
        if (ShouldSkipMiddleware(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Inicializar contexto de correlação
        InitializeCorrelationContext(context);

        // Iniciar rastreamento de performance
        using var performanceTracker = _performanceTracker.TrackDuration(
            "http.request",
            new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = GetNormalizedPath(context.Request.Path),
                ["user_agent"] = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown"
            });

        // Iniciar span de tracing
        using var span = _tracer.StartSpan($"HTTP {context.Request.Method} {GetNormalizedPath(context.Request.Path)}") as ISpanContext;

        // Verificar se o cast foi bem-sucedido
        if (span == null)
        {
            throw new InvalidOperationException("StartSpan deve retornar uma instância de ISpanContext");
        }

        // Adicionar tags ao span
        AddSpanTags(context, span);

        // Adicionar headers de resposta
        AddResponseHeaders(context);

        // Incrementar contador de requisições
        DiagnosticService.IncrementRequestCount();

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        try
        {
            // Log de início da requisição
            LogRequestStart(context);

            // Executar próximo middleware
            await _next(context);

            stopwatch.Stop();

            // Log de conclusão da requisição
            LogRequestCompletion(context, stopwatch.Elapsed);

            // O rastreamento será finalizado automaticamente pelo 'using'

            // Adicionar informações de conclusão ao span
            AddCompletionInfoToSpan(context, span, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            exception = ex;

            // Log de erro
            LogRequestError(context, ex, stopwatch.Elapsed);

            // O rastreamento será finalizado automaticamente pelo 'using'

            // Registrar exceção no span
            span.RecordException(ex);
            span.SetStatus(SpanStatus.Error, ex.Message);

            // Incrementar contador de erros
            DiagnosticService.IncrementErrorCount();

            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Decrementar contador de requisições ativas
            DiagnosticService.DecrementActiveRequests();

            // Registrar tempo de resposta
            DiagnosticService.RecordResponseTime(stopwatch.Elapsed.TotalMilliseconds);

            // Log final com métricas
            LogRequestMetrics(context, stopwatch.Elapsed, exception);
        }
    }

    private bool ShouldSkipMiddleware(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        return pathValue switch
        {
            var p when p?.Contains("/health") == true => true,
            var p when p?.Contains("/swagger") == true => true,
            var p when p?.Contains("/_framework") == true => true,
            var p when p?.Contains("/favicon.ico") == true => true,
            _ => false
        };
    }

    private void InitializeCorrelationContext(HttpContext context)
    {
        // Obter ou gerar correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                          ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString("N");

        // Configurar contexto de correlação
        _correlationContextAccessor.CorrelationId = correlationId;
        _correlationContextAccessor.RequestStartTime = DateTime.UtcNow;
        _correlationContextAccessor.IpAddress = GetClientIpAddress(context);
        _correlationContextAccessor.UserAgent = context.Request.Headers.UserAgent.FirstOrDefault();

        // Se usuário autenticado, adicionar informações do usuário
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            _correlationContextAccessor.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _correlationContextAccessor.UserName = context.User.FindFirst(ClaimTypes.Name)?.Value
                                                 ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        // Adicionar correlation ID ao contexto do HTTP
        context.Items["CorrelationId"] = correlationId;
    }

    private void AddSpanTags(HttpContext context, ISpanContext span)
    {
        span.AddTag("http.method", context.Request.Method);
        span.AddTag("http.url", $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}");
        span.AddTag("http.route", GetNormalizedPath(context.Request.Path));
        span.AddTag("http.user_agent", context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown");
        span.AddTag("client.ip", GetClientIpAddress(context));
        span.AddTag("server.name", Environment.MachineName);
        span.AddTag("correlation.id", _correlationContextAccessor.CorrelationId);

        // Adicionar informações do usuário se autenticado
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            span.AddTag("user.id", _correlationContextAccessor.UserId);
            span.AddTag("user.name", _correlationContextAccessor.UserName);
        }

        // Adicionar tamanho da requisição
        if (context.Request.ContentLength.HasValue)
        {
            span.AddTag("http.request.body.size", context.Request.ContentLength.Value);
        }
    }

    private void AddResponseHeaders(HttpContext context)
    {
        var response = context.Response;

        // Headers de correlação
        response.Headers.Append("X-Correlation-ID", _correlationContextAccessor.CorrelationId);

        // Headers informativos
        response.Headers.Append("X-BKS-Framework-Version", "1.0.0");
        response.Headers.Append("X-Server-Name", Environment.MachineName);
        response.Headers.Append("X-Request-Time", _correlationContextAccessor.RequestStartTime.ToString("O"));

        // Headers de tracing se disponível
        if (_tracer.CurrentTraceId != null)
        {
            response.Headers.Append("X-Trace-ID", _tracer.CurrentTraceId);
        }

        if (_tracer.CurrentSpanId != null)
        {
            response.Headers.Append("X-Span-ID", _tracer.CurrentSpanId);
        }

        // Headers de segurança básicos
        response.Headers.Append("X-Content-Type-Options", "nosniff");
        response.Headers.Append("X-Frame-Options", "DENY");
        response.Headers.Append("X-XSS-Protection", "1; mode=block");
    }

    // CORREÇÃO: Método corrigido para usar a interface ISpanContext completa
    private void AddCompletionInfoToSpan(HttpContext context, ISpanContext span, TimeSpan duration)
    {
        span.AddTag("http.response.status_code", context.Response.StatusCode);
        span.AddTag("http.response.duration_ms", duration.TotalMilliseconds);

        // Definir status do span baseado no código de resposta
        if (context.Response.StatusCode >= 400)
        {
            span.SetStatus(SpanStatus.Error, $"HTTP {context.Response.StatusCode}");
        }
        else
        {
            span.SetStatus(SpanStatus.Ok);
        }
    }

    private void LogRequestStart(HttpContext context)
    {
        var logLevel = DetermineLogLevel(context.Request.Path);

        _logger.LogWithCorrelation(logLevel,
            "HTTP {Method} {Path} iniciada - Client: {ClientIp} - User: {User}",
            _correlationContextAccessor.CorrelationId);

        _logger.LogStructured(logLevel,
            "Request started",
            new
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                QueryString = context.Request.QueryString.Value,
                ContentType = context.Request.ContentType,
                ContentLength = context.Request.ContentLength,
                UserAgent = _correlationContextAccessor.UserAgent,
                ClientIp = _correlationContextAccessor.IpAddress,
                UserId = _correlationContextAccessor.UserId,
                UserName = _correlationContextAccessor.UserName
            },
            _correlationContextAccessor.CorrelationId);
    }

    private void LogRequestCompletion(HttpContext context, TimeSpan duration)
    {
        var logLevel = DetermineLogLevel(context.Response.StatusCode, duration);

        _logger.LogWithCorrelation(logLevel,
            "HTTP {Method} {Path} concluída - Status: {StatusCode} - Duração: {Duration}ms",
            _correlationContextAccessor.CorrelationId);

        _logger.LogStructured(logLevel,
            "Request completed successfully",
            new
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                StatusCode = context.Response.StatusCode,
                Duration = duration.TotalMilliseconds,
                ResponseContentType = context.Response.ContentType,
                Success = context.Response.StatusCode < 400
            },
            _correlationContextAccessor.CorrelationId);
    }

    private void LogRequestError(HttpContext context, Exception exception, TimeSpan duration)
    {
        _logger.Error(exception,
            "HTTP {Method} {Path} falhou - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path.Value,
            duration.TotalMilliseconds,
            _correlationContextAccessor.CorrelationId);

        _logger.LogStructured(LogLevel.Error,
            "Request failed with exception",
            new
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                Duration = duration.TotalMilliseconds,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace
            },
            _correlationContextAccessor.CorrelationId);
    }

    private void LogRequestMetrics(HttpContext context, TimeSpan duration, Exception? exception)
    {
        var statusCategory = GetStatusCategory(context.Response.StatusCode);
        var operationType = DetermineOperationType(context.Request.Method, context.Request.Path);

        _logger.LogStructured(LogLevel.Information,
            "Request metrics",
            new
            {
                Method = context.Request.Method,
                Path = GetNormalizedPath(context.Request.Path),
                StatusCode = context.Response.StatusCode,
                StatusCategory = statusCategory,
                Duration = duration.TotalMilliseconds,
                Success = exception == null && context.Response.StatusCode < 400,
                OperationType = operationType,
                HasError = exception != null
            },
            _correlationContextAccessor.CorrelationId);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Verificar headers de proxy
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetNormalizedPath(PathString path)
    {
        var pathValue = path.Value ?? "/";

        // Normalizar IDs numéricos e GUIDs para reduzir cardinalidade
        pathValue = System.Text.RegularExpressions.Regex.Replace(pathValue, @"/\d+(/|$)", "/{id}$1");
        pathValue = System.Text.RegularExpressions.Regex.Replace(pathValue,
            @"/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}(/|$)",
            "/{guid}$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return pathValue;
    }

    private LogLevel DetermineLogLevel(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        return pathValue switch
        {
            var p when p?.Contains("/health") == true => LogLevel.Debug,
            var p when p?.Contains("/swagger") == true => LogLevel.Debug,
            _ => LogLevel.Information
        };
    }

    private LogLevel DetermineLogLevel(int statusCode, TimeSpan duration)
    {
        if (statusCode >= 500)
            return LogLevel.Error;

        if (statusCode >= 400)
            return LogLevel.Warning;

        if (duration.TotalSeconds > 5)
            return LogLevel.Warning;

        return LogLevel.Information;
    }

    private string GetStatusCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "1xx"
        };
    }

    private string DetermineOperationType(string method, PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();

        if (pathValue?.Contains("/health") == true)
            return "health_check";

        if (pathValue?.Contains("/swagger") == true)
            return "documentation";

        if (pathValue?.Contains("/api") == true)
        {
            return method.ToLowerInvariant() switch
            {
                "get" => "query",
                "post" => "command",
                "put" => "update",
                "patch" => "partial_update",
                "delete" => "delete",
                _ => "api_call"
            };
        }

        return "web_request";
    }
}

