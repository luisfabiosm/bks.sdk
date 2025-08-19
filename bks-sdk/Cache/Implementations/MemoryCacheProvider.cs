using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Cache.Implementations
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public MemoryCacheProvider(Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var options = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
        {
            _cache.TryGetValue(key, out var value);
            return Task.FromResult(value as string);
        }
    }


}
