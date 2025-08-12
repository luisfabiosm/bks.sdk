using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    public record TokenSearchCriteria
    {
       
        public DateTime? CreatedAfter { get; init; }

        public DateTime? CreatedBefore { get; init; }

  
        public DateTime? ExpiresAfter { get; init; }

   
        public DateTime? ExpiresBefore { get; init; }

        public bool IncludeExpired { get; init; } = false;

  
        public bool IncludeRevoked { get; init; } = false;

        public Dictionary<string, object> MetadataFilters { get; init; } = new();

     
        public int? MaxResults { get; init; }

    
        public int Offset { get; init; } = 0;
    }

}
