using bks.sdk.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Pipeline;


public interface IPipelineStep<TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    string StepName { get; }
    int Order { get; }
}