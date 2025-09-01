

namespace bks.sdk.Security.Licensing;

public record LicenseValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public LicenseInfo? LicenseInfo { get; init; }

    public static LicenseValidationResult Success(LicenseInfo licenseInfo)
        => new() { IsValid = true, LicenseInfo = licenseInfo };

    public static LicenseValidationResult Failure(string error)
        => new() { IsValid = false, Error = error };
}
