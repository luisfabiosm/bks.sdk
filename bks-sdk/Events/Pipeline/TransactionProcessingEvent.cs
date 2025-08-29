using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Events.Pipeline;

public record TransactionProcessingEvent : DomainEvent
{
    public string TransactionId { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string ProcessorName { get; init; } = string.Empty;
    public override string EventType => "pipeline.transaction.processing";
}


