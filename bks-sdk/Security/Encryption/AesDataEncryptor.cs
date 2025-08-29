using bks.sdk.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Encryption;

public class AesDataEncryptor : IDataEncryptor, IDisposable
{
    private readonly BKSFrameworkSettings _settings;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly Aes _aes;

    public AesDataEncryptor(BKSFrameworkSettings settings)
    {
        _settings = settings;
        _aes = Aes.Create();

        // Gerar chave a partir da secret key do JWT (para simplicidade)
        _key = DeriveKeyFromSecret(_settings.Security.Jwt.SecretKey);
        _iv = new byte[16]; // IV zerado para simplicidade (em produção, use IV aleatório)

        _aes.Key = _key;
        _aes.IV = _iv;
        _aes.Mode = CipherMode.CBC;
        _aes.Padding = PaddingMode.PKCS7;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = Encrypt(plainBytes);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro na criptografia: {ex.Message}", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = Decrypt(encryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro na descriptografia: {ex.Message}", ex);
        }
    }

    public byte[] Encrypt(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        using var encryptor = _aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

        csEncrypt.Write(data, 0, data.Length);
        csEncrypt.FlushFinalBlock();

        return msEncrypt.ToArray();
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            return Array.Empty<byte>();

        using var decryptor = _aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encryptedData);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var msOutput = new MemoryStream();

        csDecrypt.CopyTo(msOutput);
        return msOutput.ToArray();
    }

    public string EncryptSensitiveData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        // Para dados sensíveis, adicionar salt
        var salt = GenerateSalt();
        var saltedData = $"{salt}:{data}";

        return Encrypt(saltedData);
    }

    public string DecryptSensitiveData(string encryptedData)
    {
        if (string.IsNullOrWhiteSpace(encryptedData))
            return string.Empty;

        var decrypted = Decrypt(encryptedData);

        // Remover salt
        var colonIndex = decrypted.IndexOf(':');
        if (colonIndex > 0 && colonIndex < decrypted.Length - 1)
        {
            return decrypted.Substring(colonIndex + 1);
        }

        return decrypted;
    }

    private byte[] DeriveKeyFromSecret(string secret)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return hash; // SHA256 retorna 32 bytes, perfeito para AES-256
    }

    private string GenerateSalt()
    {
        var saltBytes = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    public void Dispose()
    {
        _aes?.Dispose();
    }
}


