using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Pipeline;


public record TransactionFailedEvent : DomainEvent
{
    public string TransactionId { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    public override string EventType => "pipeline.transaction.failed";
}
