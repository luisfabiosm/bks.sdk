using bks.sdk.Transactions.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions.Handlres
{
    public record TransactionAuditEvent : DomainEvent
    {
        public required TransactionAuditInfo AuditInfo { get; init; }
        public required string EventType { get; init; }
    }


}
