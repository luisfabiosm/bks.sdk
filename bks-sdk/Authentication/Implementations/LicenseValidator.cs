using bks.sdk.Core.Configuration;

namespace bks.sdk.Authentication.Implementations;

public class LicenseValidator : ILicenseValidator
{
    private readonly SDKSettings _settings;

    public LicenseValidator(SDKSettings settings)
    {

        _settings = settings;
    }
    public LicenseValidator()
    {
        _settings = new SDKSettings();
        _settings.LicenseKey = "bks-sdk-2025-productionkey-b|r0";
    }

    public bool Validate(string licenseKey, string applicationName)
    {
        return _settings.LicenseKey.Replace("-b|r0", applicationName) == string.Concat(licenseKey, applicationName);                  
            ///&&          _settings.ApplicationName == applicationName;
    }

    public bool ValidateDev(string licenseKey)
    {
        return _settings.LicenseKey == licenseKey;
    }
}