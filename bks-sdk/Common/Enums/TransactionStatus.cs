using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Enums;

public enum TransactionStatus
{
    Created = 0,
    Validating = 1,
    PreProcessing = 2,
    Processing = 3,
    PostProcessing = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
    TimedOut = 8
}
