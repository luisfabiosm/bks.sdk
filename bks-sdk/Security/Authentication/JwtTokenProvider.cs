using bks.sdk.Core.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Security.Authentication;

public class JwtTokenProvider : IJwtTokenProvider
{
    private readonly BKSFrameworkSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenProvider(BKSFrameworkSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrWhiteSpace(_settings.Security.Jwt.SecretKey))
            throw new ArgumentException("JWT SecretKey não pode ser vazio", nameof(settings));

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Security.Jwt.SecretKey));
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateToken(string subject, IEnumerable<Claim> claims)
    {
        return GenerateToken(subject, claims, null);
    }

    public string GenerateToken(string subject, IEnumerable<Claim> claims, TimeSpan? customExpiration)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject não pode ser vazio", nameof(subject));

        var expiration = customExpiration ?? TimeSpan.FromMinutes(_settings.Security.Jwt.ExpirationMinutes);
        var expires = DateTime.UtcNow.Add(expiration);

        var allClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("app", _settings.ApplicationName)
        };

        if (claims != null)
        {
            allClaims.AddRange(claims);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(allClaims),
            Expires = expires,
            Issuer = _settings.Security.Jwt.Issuer,
            Audience = _settings.Security.Jwt.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var result = ValidateTokenDetailed(token);
        return result.IsValid ? result.Principal : null;
    }

    public TokenValidationResult ValidateTokenDetailed(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return TokenValidationResult.Failure("Token é obrigatório");

        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = _settings.Security.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Security.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };

            var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            var jwtToken = validatedToken as JwtSecurityToken;
            var expiresAt = jwtToken?.ValidTo;

            return TokenValidationResult.Success(principal, expiresAt);
        }
        catch (SecurityTokenExpiredException)
        {
            return TokenValidationResult.Failure("Token expirado");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return TokenValidationResult.Failure("Assinatura do token inválida");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return TokenValidationResult.Failure("Emissor do token inválido");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return TokenValidationResult.Failure("Audiência do token inválida");
        }
        catch (Exception ex)
        {
            return TokenValidationResult.Failure($"Token inválido: {ex.Message}");
        }
    }

    public bool IsTokenExpired(string token)
    {
        var result = ValidateTokenDetailed(token);
        return !result.IsValid && result.Error?.Contains("expirado") == true;
    }

    public DateTime? GetTokenExpirationDate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }
}

