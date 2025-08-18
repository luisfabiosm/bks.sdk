using System.Security.Claims;

namespace bks.sdk.Authentication;

public interface IJwtTokenProvider
{
    string GenerateToken(string subject, IEnumerable<Claim> claims);
    ClaimsPrincipal? ValidateToken(string token);
}