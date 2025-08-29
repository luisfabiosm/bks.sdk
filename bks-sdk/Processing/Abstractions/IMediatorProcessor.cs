using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Abstractions;

public interface IMediatorProcessor<TRequest, TResponse> : IBusinessProcessor<TRequest, TResponse>
    where TRequest : class
{
}