using bks.sdk.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions
{
    public class TransactionStartedEvent : DomainEvent
    {
        public string TransactionId { get; init; } = string.Empty;
        public string TransactionType { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public override string EventType => "transaction.started";
    }
}
