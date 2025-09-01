using bks.sdk.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Initialization;


public class BKSFrameworkOptions
{
    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.Mediator;
    public bool EnableValidation { get; set; } = true;
    public bool EnableEvents { get; set; } = false;
}


