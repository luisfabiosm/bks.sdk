using StackExchange.Redis;

namespace bks.sdk.Cache.Implementations;

public class RedisCacheProvider : bks.sdk.Cache.ICacheProvider
{
    private readonly IDatabase _db;

    public RedisCacheProvider(string connectionString)
    {
        var muxer = ConnectionMultiplexer.Connect(connectionString);
        _db = muxer.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, value, expiry);

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }
}