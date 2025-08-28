using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Middlewares
{
    public class HttpMetricsMiddleware
    {
        private static readonly ActivitySource ActivitySource = new("bks.sdk.http");

        private readonly RequestDelegate _next;
        private readonly IBKSLogger _logger;
        private readonly ILogger<HttpMetricsMiddleware> _microsoftLogger;

        // Métricas customizadas
        private static readonly System.Diagnostics.Metrics.Counter<long> RequestsTotal =
            BKSMetrics.Meter.CreateCounter<long>(
                "bks_http_requests_total",
                description: "Total number of HTTP requests");

        private static readonly System.Diagnostics.Metrics.Histogram<double> RequestDuration =
            BKSMetrics.Meter.CreateHistogram<double>(
                "bks_http_request_duration_seconds",
                unit: "s",
                description: "Duration of HTTP requests");

        private static readonly System.Diagnostics.Metrics.Histogram<long> RequestSize =
            BKSMetrics.Meter.CreateHistogram<long>(
                "bks_http_request_size_bytes",
                unit: "bytes",
                description: "Size of HTTP request bodies");

        private static readonly System.Diagnostics.Metrics.Histogram<long> ResponseSize =
            BKSMetrics.Meter.CreateHistogram<long>(
                "bks_http_response_size_bytes",
                unit: "bytes",
                description: "Size of HTTP response bodies");

        private static readonly System.Diagnostics.Metrics.UpDownCounter<long> ActiveRequests =
            BKSMetrics.Meter.CreateUpDownCounter<long>(
                "bks_http_requests_active",
                description: "Number of active HTTP requests");

        public HttpMetricsMiddleware(
            RequestDelegate next,
            IBKSLogger logger,
            ILogger<HttpMetricsMiddleware> microsoftLogger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _microsoftLogger = microsoftLogger ?? throw new ArgumentNullException(nameof(microsoftLogger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip instrumentação para endpoints internos
            if (ShouldSkipInstrumentation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = GetNormalizedPath(context.Request.Path);
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
            var clientIp = GetClientIpAddress(context);

            // Criar span HTTP customizado
            using var activity = ActivitySource.StartActivity("http.request");
            activity?.SetTag("http.method", method);
            activity?.SetTag("http.url", $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}");
            activity?.SetTag("http.route", path);
            activity?.SetTag("http.user_agent", userAgent);
            activity?.SetTag("client.ip", clientIp);
            activity?.SetTag("server.name", Environment.MachineName);

            // Obter tamanho da requisição
            var requestSize = GetRequestSize(context.Request);
            if (requestSize > 0)
            {
                activity?.SetTag("http.request.body.size", requestSize);
            }

            // Incrementar contador de requisições ativas
            ActiveRequests.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("route", path));

            // Incrementar contador total de requisições
            RequestsTotal.Add(1,
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("route", path));

            // Registrar tamanho da requisição
            if (requestSize > 0)
            {
                RequestSize.Record(requestSize,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("route", path));
            }

            Exception? exception = null;
            int statusCode = 200;

            try
            {
                _logger.Trace($"HTTP {method} {path} started - Client: {clientIp}");

                // Adicionar headers customizados à resposta
                AddInstrumentationHeaders(context);

                // Interceptar response para capturar tamanho
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Executar próximo middleware
                await _next(context);

                // Capturar informações da resposta
                statusCode = context.Response.StatusCode;
                var responseSize = responseBodyStream.Length;

                // Copiar response de volta para o stream original
                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalBodyStream);

                // Registrar métricas de resposta
                ResponseSize.Record(responseSize,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("route", path),
                    new KeyValuePair<string, object?>("status_code", statusCode.ToString()));

                activity?.SetTag("http.response.status_code", statusCode);
                activity?.SetTag("http.response.body.size", responseSize);

                // Definir status do span baseado no código de resposta
                if (statusCode >= 400)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {statusCode}");
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                statusCode = 500;

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.RecordException(ex);

                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Decrementar contador de requisições ativas
                ActiveRequests.Add(-1,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("route", path));

                // Registrar duração da requisição
                var duration = stopwatch.Elapsed.TotalSeconds;
                RequestDuration.Record(duration,
                    new KeyValuePair<string, object?>("method", method),
                    new KeyValuePair<string, object?>("route", path),
                    new KeyValuePair<string, object?>("status_code", statusCode.ToString()));

                // Determinar nível de log baseado no resultado
                var logLevel = DetermineLogLevel(statusCode, duration, exception);
                var statusCategory = GetStatusCategory(statusCode);

                // Log estruturado da requisição
                var logMessage = $"HTTP {method} {path} completed - Status: {statusCode}, Duration: {duration:F3}s, Client: {clientIp}";

                switch (logLevel)
                {
                    case LogLevel.Error:
                        _logger.Error($"{logMessage} - Error: {exception?.Message}");
                        _microsoftLogger.LogError(exception,
                            "HTTP {Method} {Path} failed - Status: {StatusCode}, Duration: {Duration}ms, Client: {ClientIp}",
                            method, path, statusCode, stopwatch.ElapsedMilliseconds, clientIp);
                        break;

                    case LogLevel.Warning:
                        _logger.Warn(logMessage);
                        _microsoftLogger.LogWarning(
                            "HTTP {Method} {Path} slow/warning - Status: {StatusCode}, Duration: {Duration}ms, Client: {ClientIp}",
                            method, path, statusCode, stopwatch.ElapsedMilliseconds, clientIp);
                        break;

                    case LogLevel.Information:
                        _logger.Info(logMessage);
                        _microsoftLogger.LogInformation(
                            "HTTP {Method} {Path} completed - Status: {StatusCode}, Duration: {Duration}ms, Client: {ClientIp}",
                            method, path, statusCode, stopwatch.ElapsedMilliseconds, clientIp);
                        break;

                    default:
                        _logger.Trace(logMessage);
                        break;
                }

                // Registrar métricas adicionais para SLA tracking
                RecordSlaMetrics(method, path, statusCode, duration);
            }
        }

        /// <summary>
        /// Determina se deve pular a instrumentação para o path
        /// </summary>
        private static bool ShouldSkipInstrumentation(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant();

            return pathValue switch
            {
                var p when p?.Contains("/health") == true => true,
                var p when p?.Contains("/metrics") == true => true,
                var p when p?.Contains("/swagger") == true => true,
                var p when p?.Contains("/_framework") == true => true,
                var p when p?.Contains("/favicon.ico") == true => true,
                _ => false
            };
        }

        /// <summary>
        /// Normaliza o path da requisição para agrupamento de métricas
        /// </summary>
        private static string GetNormalizedPath(PathString path)
        {
            var pathValue = path.Value ?? "/";

            // Normalizar IDs numéricos para reduzir cardinalidade
            pathValue = System.Text.RegularExpressions.Regex.Replace(pathValue, @"/\d+(/|$)", "/{id}$1");

            // Normalizar GUIDs
            pathValue = System.Text.RegularExpressions.Regex.Replace(pathValue,
                @"/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}(/|$)",
                "/{guid}$1",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return pathValue;
        }

        /// <summary>
        /// Obtém o endereço IP real do cliente
        /// </summary>
        private static string GetClientIpAddress(HttpContext context)
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

        /// <summary>
        /// Calcula o tamanho da requisição
        /// </summary>
        private static long GetRequestSize(HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                return request.ContentLength.Value;
            }

            // Estimar baseado nos headers
            var headerSize = request.Headers.Sum(h =>
                Encoding.UTF8.GetByteCount($"{h.Key}: {string.Join(", ", h.Value)}\r\n"));

            return headerSize;
        }

        /// <summary>
        /// Adiciona headers de instrumentação à resposta
        /// </summary>
        private static void AddInstrumentationHeaders(HttpContext context)
        {
            // Headers já adicionados pelo TransactionCorrelationMiddleware
            if (!context.Response.Headers.ContainsKey("X-Server-Name"))
            {
                context.Response.Headers.Append("X-Server-Name", Environment.MachineName);
            }

            if (!context.Response.Headers.ContainsKey("X-BKS-SDK-Version"))
            {
                context.Response.Headers.Append("X-BKS-SDK-Version", "1.0.3");
            }

            // Adicionar trace ID se disponível
            if (Activity.Current != null && !context.Response.Headers.ContainsKey("X-Trace-ID"))
            {
                context.Response.Headers.Append("X-Trace-ID", Activity.Current.TraceId.ToString());
                context.Response.Headers.Append("X-Span-ID", Activity.Current.SpanId.ToString());
            }
        }

        /// <summary>
        /// Determina o nível de log baseado na resposta
        /// </summary>
        private static LogLevel DetermineLogLevel(int statusCode, double duration, Exception? exception)
        {
            // Erro
            if (exception != null || statusCode >= 500)
                return LogLevel.Error;

            // Warning para requisições lentas ou client errors
            if (duration > 1.0 || statusCode >= 400)
                return LogLevel.Warning;

            // Information para requisições normais
            if (statusCode < 400)
                return LogLevel.Information;

            return LogLevel.Debug;
        }

        /// <summary>
        /// Obtém a categoria do status para métricas
        /// </summary>
        private static string GetStatusCategory(int statusCode)
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

        /// <summary>
        /// Registra métricas específicas para SLA tracking
        /// </summary>
        private void RecordSlaMetrics(string method, string path, int statusCode, double duration)
        {
            try
            {
                var statusCategory = GetStatusCategory(statusCode);

                // Métrica de disponibilidade (uptime)
                var isHealthy = statusCode < 500;
                BKSMetrics.Meter.CreateCounter<long>("bks_http_requests_by_status")
                    .Add(1,
                        new KeyValuePair<string, object?>("method", method),
                        new KeyValuePair<string, object?>("route", path),
                        new KeyValuePair<string, object?>("status_category", statusCategory),
                        new KeyValuePair<string, object?>("healthy", isHealthy.ToString().ToLower()));

                // Métricas de SLA por tempo de resposta
                var slaTarget = GetSlaTarget(path);
                var meetsSla = duration <= slaTarget;

                BKSMetrics.Meter.CreateCounter<long>("bks_http_requests_sla")
                    .Add(1,
                        new KeyValuePair<string, object?>("method", method),
                        new KeyValuePair<string, object?>("route", path),
                        new KeyValuePair<string, object?>("sla_met", meetsSla.ToString().ToLower()),
                        new KeyValuePair<string, object?>("sla_target", slaTarget.ToString()));

                // Métricas de percentil para diferentes tipos de operação
                var operationType = GetOperationType(method, path);
                BKSMetrics.Meter.CreateHistogram<double>("bks_http_operation_duration_seconds")
                    .Record(duration,
                        new KeyValuePair<string, object?>("operation_type", operationType),
                        new KeyValuePair<string, object?>("route", path));
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to record SLA metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém o SLA target baseado no endpoint
        /// </summary>
        private static double GetSlaTarget(string path)
        {
            return path switch
            {
                var p when p.Contains("/health") => 0.1, // 100ms para health checks
                var p when p.Contains("/api/transacoes") => 2.0, // 2s para transações
                var p when p.Contains("/api") => 1.0, // 1s para APIs gerais
                _ => 5.0 // 5s padrão
            };
        }

        /// <summary>
        /// Classifica o tipo de operação para métricas
        /// </summary>
        private static string GetOperationType(string method, string path)
        {
            if (path.Contains("/health"))
                return "health_check";

            if (path.Contains("/info"))
                return "info";

            if (path.Contains("/transacoes"))
                return "transaction";

            if (path.Contains("/api"))
                return method.ToLowerInvariant() switch
                {
                    "get" => "query",
                    "post" => "command",
                    "put" => "update",
                    "patch" => "partial_update",
                    "delete" => "delete",
                    _ => "api_call"
                };

            return "web_request";
        }
    }
}
