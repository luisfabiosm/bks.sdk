using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    public record SecureToken
    {

        public required string Token { get; set; }


        public required string TokenId { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  
        public DateTime? ExpiresAt { get; set; }


        public required string DataHash { get; init; }


        public Dictionary<string, object> Metadata { get; init; } = new();


        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

        public bool IsValid => !IsExpired;
    }

}
