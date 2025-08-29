using bks.sdk.Observability.Correlation;
using bks.sdk.Observability.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bks.sdk.Middlewares.ExceptionHandling;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBKSLogger _logger;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        IBKSLogger logger,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _next = next;
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.Error(ex,
                "Exceção não tratada capturada pelo middleware global - CorrelationId: {CorrelationId}",
                _correlationContextAccessor.CorrelationId);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResult = exception switch
        {
            ValidationException validationEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Validation Error",
                validationEx.Errors),

            UnauthorizedAccessException => CreateErrorResponse(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Access denied"),

            ArgumentException argEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Bad Request",
                argEx.Message),

            InvalidOperationException invalidEx => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidEx.Message),

            NotImplementedException => CreateErrorResponse(
                HttpStatusCode.NotImplemented,
                "Not Implemented",
                "The requested functionality is not implemented"),

            TimeoutException => CreateErrorResponse(
                HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "The request timed out"),

            _ => CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred")
        };

        response.StatusCode = (int)errorResult.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResult.Error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }

    private (HttpStatusCode StatusCode, ErrorResponse Error) CreateErrorResponse(
        HttpStatusCode statusCode,
        string title,
        object detail)
    {
        var error = new ErrorResponse
        {
            Title = title,
            Status = (int)statusCode,
            Detail = detail?.ToString() ?? "No details available",
            CorrelationId = _correlationContextAccessor.CorrelationId,
            Timestamp = DateTime.UtcNow,
            Instance = $"{Environment.MachineName}:{Environment.ProcessId}"
        };

        // Se detail é uma lista de erros de validação
        if (detail is IEnumerable<string> errors)
        {
            error.Errors = errors.ToList();
            error.Detail = $"{errors.Count()} validation error(s) occurred";
        }

        return (statusCode, error);
    }

    private class ErrorResponse
    {
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Instance { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }
    }
}
