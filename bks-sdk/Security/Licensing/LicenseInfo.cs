using bks.sdk.Security.Authentication.bks.sdk.Security.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Licensing;


public record LicenseInfo
{
    public string LicenseKey { get; init; } = string.Empty;
    public string ApplicationName { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public LicenseType Type { get; init; }
    public Dictionary<string, string> Features { get; init; } = new();
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}