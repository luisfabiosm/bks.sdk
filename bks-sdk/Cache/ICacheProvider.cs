namespace bks.sdk.Cache;

public interface ICacheProvider
{
    Task SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetAsync(string key);
}