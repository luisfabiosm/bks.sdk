
using System.Text;
using System.Text.Json;
using bks.sdk.Authentication;
using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;

namespace bks.sdk.Transactions
{
    public interface ITransactionTokenService
    {
        Task<Result<TransactionTokenData>> RecoverTransactionAsync(string token);
        Task<Result<T>> RecoverTransactionAsync<T>(string token) where T : BaseTransaction;
        Task<bool> ValidateTokenAsync(string token);
    }
}
