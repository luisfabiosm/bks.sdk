using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Mediator
{
    public interface ITransactionHandler<in TTransaction, TResponse>
         where TTransaction : ITransaction<TResponse>
    {
        ValueTask<TResponse> Handle(TTransaction transaction, CancellationToken cancellationToken = default);
    }
}
