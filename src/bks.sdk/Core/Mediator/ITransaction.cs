using System.Threading;
using System.Threading.Tasks;


namespace bks.sdk.Core.Mediator
{
    public interface ITransaction<TResponse>
    {

        string TransactionId { get; }

        string CorrelationId { get; }


        DateTimeOffset CreatedAt { get; }

        Dictionary<string, object> Metadata { get; }
    }

}
