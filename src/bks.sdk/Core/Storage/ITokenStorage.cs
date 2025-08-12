using bks.sdk.Core.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    public interface ITokenStorage : IDisposable
    {

        ValueTask StoreTokenAsync(SecureToken token, CancellationToken cancellationToken = default);

   
        ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default);

        ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default);

    
        ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default);


        ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default);

     
        ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);


        ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

}
