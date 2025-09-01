

namespace bks.sdk.Security.Encryption;

public record EncryptionResult
{
    public bool Success { get; init; }
    public string? Data { get; init; }
    public string? Error { get; init; }

    public static EncryptionResult CompleteSuccess(string data) => new() { Success = true, Data = data };
    public static EncryptionResult Failure(string error) => new() { Success = false, Error = error };
}

