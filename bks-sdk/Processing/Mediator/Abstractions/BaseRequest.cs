using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Mediator.Abstractions;

public abstract record BaseRequest<TResponse> : IRequest<TResponse>
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}