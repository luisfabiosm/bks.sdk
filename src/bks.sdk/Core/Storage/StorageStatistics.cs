using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    public record StorageStatistics
    {
     
        public long TotalTokens { get; init; }
     
        public long ValidTokens { get; init; }

        public long ExpiredTokens { get; init; }

 
        public long RevokedTokens { get; init; }

        public long StorageSize { get; init; }

 
        public DateTime? LastCleanup { get; init; }
    }

}
