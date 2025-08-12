using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Mediator
{
    public record HandlerInfo
    {
        public required Type TransactionType { get; init; }
        public required Type ResponseType { get; init; }
        public required Type HandlerType { get; init; }
        public required string HandlerName { get; init; }
    }
}
