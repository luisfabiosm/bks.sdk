using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    internal sealed class RedisConfigurationProvider : IConfigurationProvider
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connection;
        private readonly string _keyPrefix;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public RedisConfigurationProvider(IConnectionMultiplexer connection, IOptions<BksConfiguration> options)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _database = _connection.GetDatabase();
            _keyPrefix = options.Value.RedisKeyPrefix;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var fullKey = GetFullKey(key);
                var value = await _database.StringGetAsync(fullKey);

                if (!value.HasValue)
                    return null;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        {
            var fullKey = GetFullKey(key);
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(fullKey, json);
        }

        public async ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
        {
            var fullKey = GetFullKey(key);
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(fullKey, json, expiration);
        }

        public async ValueTask<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyDeleteAsync(fullKey);
        }

        public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }

        public async ValueTask<IEnumerable<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var pattern = GetFullKey(prefix) + "*";
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);

            return keys.Select(k => k.ToString().Substring(_keyPrefix.Length)).ToList();
        }

        private string GetFullKey(string key) => $"{_keyPrefix}{key}";

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }

}
