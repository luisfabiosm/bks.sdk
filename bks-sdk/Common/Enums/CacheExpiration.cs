using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Enums;


public enum CacheExpiration
{
    Short = 300,      // 5 minutes
    Medium = 1800,    // 30 minutes
    Long = 3600,      // 1 hour
    Extended = 86400  // 24 hours
}
