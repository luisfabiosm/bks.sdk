using bks.sdk.Core.Configuration;

namespace bks.sdk.Authentication.Implementations;

public class LicenseValidator : ILicenseValidator
{
    private readonly SDKSettings _settings;

    public LicenseValidator(SDKSettings settings)
    {
        _settings = settings;
    }

    public bool Validate(string licenseKey, string applicationName)
    {
        return _settings.LicenseKey == licenseKey &&
               _settings.ApplicationName == applicationName;
    }
}