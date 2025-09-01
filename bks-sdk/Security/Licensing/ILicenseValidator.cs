
namespace bks.sdk.Security.Licensing;

public interface ILicenseValidator
{
    bool ValidateLicense(string licenseKey, string applicationName);
    LicenseValidationResult ValidateLicenseDetailed(string licenseKey, string applicationName);
    bool IsLicenseExpired(string licenseKey);
    DateTime? GetLicenseExpirationDate(string licenseKey);
    LicenseInfo? GetLicenseInfo(string licenseKey);
}
