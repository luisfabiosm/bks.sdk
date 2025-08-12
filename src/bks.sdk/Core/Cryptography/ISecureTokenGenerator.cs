using System;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    public interface ISecureTokenGenerator
    {
      
        ValueTask<SecureToken> GenerateTokenAsync<T>(T data, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

 
        ValueTask<T?> RetrieveDataAsync<T>(string token, CancellationToken cancellationToken = default) where T : class;


        ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);


        ValueTask<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    }

}
