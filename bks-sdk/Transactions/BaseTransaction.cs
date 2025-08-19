
using bks.sdk.Common.Results;
using bks.sdk.Enum;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Transactions;

public abstract record BaseTransaction
{

    public string CorrelationId { get; private init; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    public string IntegrityHash { get; private init; } = string.Empty;

    public TransactionStatus Status { get; internal set; } = TransactionStatus.Created;

    public Dictionary<string, object> Metadata { get; private init; } = new();

    protected BaseTransaction()
    {
        IntegrityHash = GenerateIntegrityHash();
    }

    public static T Create<T>() where T : BaseTransaction, new()
    {
        return new T();
    }

    public virtual ValidationResult ValidateTransaction()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CorrelationId))
            errors.Add("CorrelationId é obrigatório");

        if (CreatedAt == default)
            errors.Add("CreatedAt deve ser válido");

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    public string GenerateToken()
    {
        var data = new
        {
            CorrelationId,
            Type = GetType().Name,
            CreatedAt = CreatedAt.ToString("O"),
            IntegrityHash,
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Serialize()))
        };

        var jsonData = JsonSerializer.Serialize(data);
        var encryptedData = EncryptData(jsonData);

        return Convert.ToBase64String(encryptedData);
    }


    public string Serialize()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(this, GetType(), options);
    }


    public static T? Deserialize<T>(string json) where T : BaseTransaction
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            return null;
        }
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value))
        {
            try
            {
                return (T)value;
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

 
    public bool VerifyIntegrity()
    {
        var currentHash = GenerateIntegrityHash();
        return string.Equals(IntegrityHash, currentHash, StringComparison.Ordinal);
    }


    private string GenerateIntegrityHash()
    {
        var data = $"{CorrelationId}|{GetType().Name}|{CreatedAt:O}|{JsonSerializer.Serialize(this, GetType())}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }


    private static byte[] EncryptData(string data)
    {
        // Usar chave fixa para demonstração - em produção, usar chave do SDK
        var key = Encoding.UTF8.GetBytes("BKS-SDK-2025-KEY-32-BYTES-LONG!"); // 32 bytes
        var iv = new byte[16]; // IV zero para demonstração

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
    }


    internal static string DecryptData(byte[] encryptedData)
    {
        var key = Encoding.UTF8.GetBytes("BKS-SDK-2025-KEY-32-BYTES-LONG!");
        var iv = new byte[16];

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}