using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Mediator
{
    public interface IBksMediator
    {

        ValueTask<TResponse> Send<TResponse>(ITransaction<TResponse> transaction, CancellationToken cancellationToken = default);


        ValueTask Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;


        bool HasHandler<TResponse>(ITransaction<TResponse> transaction);

        IEnumerable<HandlerInfo> GetRegisteredHandlers();
    }

}
