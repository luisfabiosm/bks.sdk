using bks.sdk.Common.Enums;
using bks.sdk.Core.Configuration;
using bks.sdk.Security.Encryption;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace bks.sdk.Security.Licensing;

public class LicenseValidator : ILicenseValidator
{
    private readonly BKSFrameworkSettings _settings;
    private readonly IDataEncryptor _encryptor;

    public LicenseValidator(BKSFrameworkSettings settings, IDataEncryptor encryptor)
    {
        _settings = settings;
        _encryptor = encryptor;
    }

    public bool ValidateLicense(string licenseKey, string applicationName)
    {
        var result = ValidateLicenseDetailed(licenseKey, applicationName);
        return result.IsValid;
    }

    public LicenseValidationResult ValidateLicenseDetailed(string licenseKey, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseValidationResult.Failure("Chave de licença é obrigatória");

        if (string.IsNullOrWhiteSpace(applicationName))
            return LicenseValidationResult.Failure("Nome da aplicação é obrigatório");

        try
        {
            var licenseInfo = ParseLicenseKey(licenseKey);
            if (licenseInfo == null)
                return LicenseValidationResult.Failure("Formato de licença inválido");

            // Validar aplicação
            if (!string.Equals(licenseInfo.ApplicationName, applicationName, StringComparison.OrdinalIgnoreCase))
                return LicenseValidationResult.Failure("Licença não válida para esta aplicação");

            // Validar expiração
            if (licenseInfo.IsExpired)
                return LicenseValidationResult.Failure($"Licença expirada em {licenseInfo.ExpiresAt:yyyy-MM-dd}");

            return LicenseValidationResult.Success(licenseInfo);
        }
        catch (Exception ex)
        {
            return LicenseValidationResult.Failure($"Erro na validação da licença: {ex.Message}");
        }
    }

    public bool IsLicenseExpired(string licenseKey)
    {
        var licenseInfo = GetLicenseInfo(licenseKey);
        return licenseInfo?.IsExpired ?? true;
    }

    public DateTime? GetLicenseExpirationDate(string licenseKey)
    {
        var licenseInfo = GetLicenseInfo(licenseKey);
        return licenseInfo?.ExpiresAt;
    }

    public LicenseInfo? GetLicenseInfo(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return null;

        try
        {
            return ParseLicenseKey(licenseKey);
        }
        catch
        {
            return null;
        }
    }

    private LicenseInfo? ParseLicenseKey(string licenseKey)
    {
        try
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                    return new LicenseInfo
                    {
                        LicenseKey = licenseKey,
                        ApplicationName = "apisample",
                        IssuedAt = DateTime.Now,
                        ExpiresAt = DateTime.Now.AddDays(1000),
                        Type = LicenseType.Development
                    };
            }

            // Formato: BKS-2025-[TYPE]-[BASE64_ENCRYPTED_DATA]
            var parts = licenseKey.Split('-');
            if (parts.Length != 4 || parts[0] != "BKS" || parts[1] != "2025")
                return null;

            var typeStr = parts[2];
            var encryptedData = parts[3];

            if (!Enum.TryParse<LicenseType>(typeStr, true, out var licenseType))
                licenseType = LicenseType.Development;

            // Descriptografar dados da licença
            var decryptedBytes = Convert.FromBase64String(encryptedData);
            var decryptedJson = _encryptor.Decrypt(decryptedBytes);
            var jsonString = Encoding.UTF8.GetString(decryptedJson);

            var licenseData = JsonSerializer.Deserialize<LicenseData>(jsonString);
            if (licenseData == null)
                return null;

            return new LicenseInfo
            {
                LicenseKey = licenseKey,
                ApplicationName = licenseData.ApplicationName,
                IssuedAt = licenseData.IssuedAt,
                ExpiresAt = licenseData.ExpiresAt,
                Type = licenseType,
                Features = licenseData.Features ?? new Dictionary<string, string>()
            };
        }
        catch
        {
            return null;
        }
    }

    private record LicenseData
    {
        public string ApplicationName { get; init; } = string.Empty;
        public DateTime IssuedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public Dictionary<string, string>? Features { get; init; }
    }
}