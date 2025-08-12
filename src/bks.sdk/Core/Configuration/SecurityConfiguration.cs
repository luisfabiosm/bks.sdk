using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record SecurityConfiguration
    {

        public string? EncryptionKey { get; init; }


        public int CacheExpirationMinutes { get; init; } = 15;


        public int CacheSlidingExpirationMinutes { get; init; } = 5;

        public bool TrackLastAccess { get; init; } = true;

  
        public bool EnableAuditTrail { get; init; } = true;


        public string HashSalt { get; init; } = "BKS_SDK_DEFAULT_SALT_2034";


        public int HashIterations { get; init; } = 100000;
    }

}
