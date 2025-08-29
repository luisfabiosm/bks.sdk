using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Pipeline;

public record TransactionStartedEvent : DomainEvent
{
    public string TransactionId { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public override string EventType => "pipeline.transaction.started";
}

