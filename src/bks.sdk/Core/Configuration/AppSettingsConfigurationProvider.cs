using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    internal sealed class AppSettingsConfigurationProvider : IConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, object> _cache;
        private readonly object _lockObject = new();

        public AppSettingsConfigurationProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = new Dictionary<string, object>();
        }

        public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var cached) && cached is T result)
                {
                    return ValueTask.FromResult<T?>(result);
                }

                var section = _configuration.GetSection($"BKS:{key.Replace(':', '.')}");
                if (!section.Exists())
                {
                    return ValueTask.FromResult<T?>(null);
                }

                var value = section.Get<T>();
                if (value != null)
                {
                    _cache[key] = value;
                }

                return ValueTask.FromResult(value);
            }
        }

        public ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        {
            lock (_lockObject)
            {
                _cache[key] = value;
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
        {
            // AppSettings não suporta expiração, apenas armazena em cache
            return SetAsync(key, value, cancellationToken);
        }

        public ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                return ValueTask.FromResult(_cache.Remove(key));
            }
        }

        public ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (_cache.ContainsKey(key))
                    return ValueTask.FromResult(true);

                var section = _configuration.GetSection($"BKS:{key.Replace(':', '.')}");
                return ValueTask.FromResult(section.Exists());
            }
        }

        public ValueTask<IEnumerable<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                var keys = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
                return ValueTask.FromResult<IEnumerable<string>>(keys);
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }
    }


}
