using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace bks.sdk.Transactions.Base;


public record TransactionResult<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorDetail { get; init; }

    public required string TransactionId { get; init; }

    public required string CorrelationId { get; init; }


    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;


    public TimeSpan ProcessingDuration { get; init; }


    public Dictionary<string, object> Metadata { get; init; } = new();

    public string? SecureToken { get; init; }


    public List<string> Warnings { get; init; } = new();


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? DebugInfo { get; init; }


    public static TransactionResult<T> CreateSuccess(
        T data,
        string transactionId,
        string correlationId,
        string? message = null,
        TimeSpan? processingDuration = null,
        string? secureToken = null)
    {
        return new TransactionResult<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Transaction completed successfully",
            TransactionId = transactionId,
            CorrelationId = correlationId,
            ProcessingDuration = processingDuration ?? TimeSpan.Zero,
            SecureToken = secureToken
        };
    }


    public static TransactionResult<T> Error(
        string transactionId,
        string correlationId,
        string message,
        string? errorCode = null,
        string? errorDetail = null,
        TimeSpan? processingDuration = null)
    {
        return new TransactionResult<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            ErrorDetail = errorDetail,
            TransactionId = transactionId,
            CorrelationId = correlationId,
            ProcessingDuration = processingDuration ?? TimeSpan.Zero
        };
    }


    public static TransactionResult<T> FromException(
        Exception exception,
        string transactionId,
        string correlationId,
        TimeSpan? processingDuration = null)
    {
        return new TransactionResult<T>
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = exception.GetType().Name,
            ErrorDetail = exception.ToString(),
            TransactionId = transactionId,
            CorrelationId = correlationId,
            ProcessingDuration = processingDuration ?? TimeSpan.Zero
        };
    }


    public TransactionResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!Success)
        {
            return new TransactionResult<TNew>
            {
                Success = false,
                Message = Message,
                ErrorCode = ErrorCode,
                ErrorDetail = ErrorDetail,
                TransactionId = TransactionId,
                CorrelationId = CorrelationId,
                ProcessedAt = ProcessedAt,
                ProcessingDuration = ProcessingDuration,
                Metadata = Metadata,
                Warnings = Warnings
            };
        }

        try
        {
            var newData = Data != null ? mapper(Data) : default;
            return new TransactionResult<TNew>
            {
                Success = true,
                Data = newData,
                Message = Message,
                TransactionId = TransactionId,
                CorrelationId = CorrelationId,
                ProcessedAt = ProcessedAt,
                ProcessingDuration = ProcessingDuration,
                Metadata = Metadata,
                SecureToken = SecureToken,
                Warnings = Warnings
            };
        }
        catch (Exception ex)
        {
            return TransactionResult<TNew>.FromException(ex, TransactionId, CorrelationId, ProcessingDuration);
        }
    }


    public TransactionResult<T> WithWarning(string warning)
    {
        var newWarnings = new List<string>(Warnings) { warning };
        return this with { Warnings = newWarnings };
    }


    public TransactionResult<T> WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata) { [key] = value };
        return this with { Metadata = newMetadata };
    }
}



