using bks.sdk.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Abstractions;

public interface IBKSBusinessProcessor<TRequest, TResponse>
    where TRequest : class
{
    Task<Result<TResponse>> ProcessAsync(TRequest request, CancellationToken cancellationToken = default);
    string ProcessorName { get; }
}
