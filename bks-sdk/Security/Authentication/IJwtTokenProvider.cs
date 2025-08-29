using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Authentication;

public interface IJwtTokenProvider
{
    string GenerateToken(string subject, IEnumerable<Claim> claims);
    string GenerateToken(string subject, IEnumerable<Claim> claims, TimeSpan? customExpiration);
    ClaimsPrincipal? ValidateToken(string token);
    bool IsTokenExpired(string token);
    DateTime? GetTokenExpirationDate(string token);
}
