using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Enum
{
    public enum TransactionStatus
    {
        Created = 0,
        PreProcessing = 1,
        Processing = 2,
        PostProcessing = 3,
        Completed = 4,
        Failed = 5,
        Cancelled = 6
    }
}
