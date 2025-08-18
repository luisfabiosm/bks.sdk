using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Transactions;

public abstract record BaseTransaction
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static BaseTransaction Factory() => throw new NotImplementedException("Deve ser implementado na transação específica.");

    public string Serialize() => JsonSerializer.Serialize(this);

    public static T? Deserialize<T>(string json) where T : BaseTransaction
        => JsonSerializer.Deserialize<T>(json);

    public string Tokenize()
    {
        var json = Serialize();
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    public static T? Recover<T>(string token, string json, Func<string, bool> validateToken) where T : BaseTransaction
    {
        if (!validateToken(token)) return null;
        var obj = Deserialize<T>(json);
        return obj?.Tokenize() == token ? obj : null;
    }
}