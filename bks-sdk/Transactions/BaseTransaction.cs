using bks.sdk.Common.Results;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Transactions;

/// <summary>
/// Classe base para todas as transa��es do sistema
/// </summary>
public abstract record BaseTransaction
{
  
    /// <summary>
    /// ID de correla��o para rastreamento distribu�do
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Data e hora de cria��o da transa��o
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Status atual da transa��o
    /// </summary>
    public bks.sdk.Enum.TransactionStatus Status { get; init; } = bks.sdk.Enum.TransactionStatus.Created;

    /// <summary>
    /// Metadata adicional da transa��o
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Chave privada para criptografia (deve ser configurada via SDK)
    /// </summary>
    private static readonly byte[] DefaultEncryptionKey = Encoding.UTF8.GetBytes("bks-sdk-2025-default-encryption-key32"); // 32 bytes para AES-256

    /// <summary>
    /// Serializa a transa��o para JSON
    /// </summary>
    /// <returns>JSON representando a transa��o</returns>
    public string Serialize()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(this, GetType(), options);
    }

    public virtual ValidationResult ValidateTransaction()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CorrelationId))
            errors.Add("CorrelationId � obrigat�rio");

        if (CreatedAt == default)
            errors.Add("CreatedAt deve ser v�lido");

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Deserializa uma transa��o a partir de JSON
    /// </summary>
    /// <typeparam name="T">Tipo da transa��o</typeparam>
    /// <param name="json">JSON da transa��o</param>
    /// <returns>Inst�ncia da transa��o ou null se inv�lida</returns>
    public static T? Deserialize<T>(string json) where T : BaseTransaction
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Criptografa dados usando AES-256
    /// </summary>
    /// <param name="plainText">Texto a ser criptografado</param>
    /// <param name="encryptionKey">Chave de criptografia (opcional, usa padr�o se n�o fornecida)</param>
    /// <returns>Dados criptografados</returns>
    public static byte[] EncryptData(string plainText, byte[]? encryptionKey = null)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        var key = encryptionKey ?? DefaultEncryptionKey;

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();

        // Escrever o IV no in�cio do stream
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return msEncrypt.ToArray();
    }

    /// <summary>
    /// Descriptografa dados usando AES-256
    /// </summary>
    /// <param name="cipherData">Dados criptografados</param>
    /// <param name="encryptionKey">Chave de criptografia (opcional, usa padr�o se n�o fornecida)</param>
    /// <returns>Texto descriptografado</returns>
    public static string DecryptData(byte[] cipherData, byte[]? encryptionKey = null)
    {
        if (cipherData == null || cipherData.Length == 0)
            throw new ArgumentException("Cipher data cannot be null or empty", nameof(cipherData));

        var key = encryptionKey ?? DefaultEncryptionKey;

        using var aes = Aes.Create();
        aes.Key = key;

        using var msDecrypt = new MemoryStream(cipherData);

        // Ler o IV do in�cio do stream
        var iv = new byte[aes.IV.Length];
        msDecrypt.Read(iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    /// <summary>
    /// Gera um token criptografado da transa��o
    /// </summary>
    /// <returns>Token criptografado base64</returns>
    public string GenerateToken()
    {
        var tokenData = new TransactionTokenData
        {
            CorrelationId = CorrelationId,
            Type = GetType().Name,
            CreatedAt = CreatedAt.ToString("O"),
            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Serialize())),
            IntegrityHash = GenerateIntegrityHash()
        };

        var tokenJson = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var encryptedData = EncryptData(tokenJson);
        return Convert.ToBase64String(encryptedData);
    }

    /// <summary>
    /// Gera um hash de integridade para a transa��o
    /// </summary>
    /// <returns>Hash SHA-256 da transa��o</returns>
    private string GenerateIntegrityHash()
    {
        var dataToHash = $"{CorrelationId}|{CreatedAt:O}|{GetType().Name}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Cria uma nova transa��o com status atualizado
    /// </summary>
    /// <param name="newStatus">Novo status</param>
    /// <returns>Nova inst�ncia da transa��o com status atualizado</returns>
    public virtual BaseTransaction WithStatus(bks.sdk.Enum.TransactionStatus newStatus)
    {
        return this with { Status = newStatus };
    }

    /// <summary>
    /// Adiciona metadata � transa��o
    /// </summary>
    /// <param name="key">Chave do metadata</param>
    /// <param name="value">Valor do metadata</param>
    /// <returns>Nova inst�ncia da transa��o com metadata adicionado</returns>
    public virtual BaseTransaction WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata)
        {
            [key] = value
        };
        return this with { Metadata = newMetadata };
    }

    /// <summary>
    /// Obt�m um valor de metadata tipado
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave do metadata</param>
    /// <returns>Valor tipado ou valor padr�o</returns>
    public T? GetMetadata<T>(string key)
    {
        if (!Metadata.TryGetValue(key, out var value))
            return default;

        try
        {
            if (value is T typedValue)
                return typedValue;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.Deserialize<T>();
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Verifica se a transa��o possui um metadata espec�fico
    /// </summary>
    /// <param name="key">Chave do metadata</param>
    /// <returns>True se existe, false caso contr�rio</returns>
    public bool HasMetadata(string key) => Metadata.ContainsKey(key);

    /// <summary>
    /// Representa��o string da transa��o para logs
    /// </summary>
    /// <returns>String representando a transa��o</returns>
    public override string ToString()
    {
        return $"{GetType().Name} {{ CorrelationId: {CorrelationId}, Status: {Status}, CreatedAt: {CreatedAt:yyyy-MM-dd HH:mm:ss} }}";
    }
}