using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Authentication;
public record TokenValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public ClaimsPrincipal? Principal { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static TokenValidationResult Success(ClaimsPrincipal principal, DateTime? expiresAt = null)
        => new() { IsValid = true, Principal = principal, ExpiresAt = expiresAt };

    public static TokenValidationResult Failure(string error)
        => new() { IsValid = false, Error = error };
}


