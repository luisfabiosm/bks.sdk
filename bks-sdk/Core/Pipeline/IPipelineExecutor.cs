using bks.sdk.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Pipeline;

public interface IPipelineExecutor
{
    Task<Result<TResponse>> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class;
}