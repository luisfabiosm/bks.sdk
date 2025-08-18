namespace bks.sdk.Authentication;

public interface ILicenseValidator
{
    bool Validate(string licenseKey, string applicationName);
}