using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record JwtConfiguration
    {

        [Required]
        public required string SecretKey { get; init; }


        public string Issuer { get; init; } = "BKS.SDK";


        public string Audience { get; init; } = "BKS.SDK.Clients";


        public TimeSpan TokenExpiration { get; init; } = TimeSpan.FromHours(1);


        public string Algorithm { get; init; } = "HS256";

        public bool EnableRefreshTokens { get; init; } = true;


        public TimeSpan RefreshTokenExpiration { get; init; } = TimeSpan.FromDays(7);


        public Dictionary<string, string> CustomClaims { get; init; } = new();
    }

}
