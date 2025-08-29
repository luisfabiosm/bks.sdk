using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Pipeline;
public record TransactionCompletedEvent : DomainEvent
{
    public string TransactionId { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan Duration { get; init; }
    public override string EventType => "pipeline.transaction.completed";
}

