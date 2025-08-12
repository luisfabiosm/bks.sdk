using System;
using System.Threading;
using System.Threading.Tasks;


namespace bks.sdk.Core.Configuration
{
    public interface IConfigurationProvider : IDisposable
    {

        ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;


        ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;

     
        ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;

  
        ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default);
    }


}
