using bks.sdk.Core.Configuration;
using bks.sdk.Core.Cryptography;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    internal sealed class RedisTokenStorage : ITokenStorage
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connection;
        private readonly string _keyPrefix;
        private readonly IBksLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public RedisTokenStorage(IOptions<BksConfiguration> configuration, IBksLogger logger)
        {
            var config = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            var connectionString = config.RedisConnectionString ??
                throw new InvalidOperationException("Redis connection string not configured");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connection = ConnectionMultiplexer.Connect(connectionString);
            _database = _connection.GetDatabase();
            _keyPrefix = $"{config.RedisKeyPrefix}tokens:";

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async ValueTask StoreTokenAsync(SecureToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var key = GetTokenKey(token.TokenId);
            var data = new TokenData
            {
                Token = token.Token,
                DataHash = token.DataHash,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                Metadata = token.Metadata,
                IsRevoked = false,
                StoredAt = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(data, _jsonOptions);

            // Definir expiração se especificada
            var expiry = token.ExpiresAt?.Subtract(DateTimeOffset.UtcNow);
            if (expiry.HasValue && expiry.Value > TimeSpan.Zero)
            {
                await _database.StringSetAsync(key, json, expiry.Value);
            }
            else
            {
                await _database.StringSetAsync(key, json);
            }

            // Adicionar ao índice de tokens
            await _database.SetAddAsync($"{_keyPrefix}index", token.TokenId);

            _logger.LogDebug("Token stored in Redis: {TokenId}", token.TokenId);
        }

        public async ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return null;

            var key = GetTokenKey(tokenId);
            var json = await _database.StringGetAsync(key);

            if (!json.HasValue)
                return null;

            var data = JsonSerializer.Deserialize<TokenData>(json!, _jsonOptions);
            if (data == null || data.IsRevoked)
                return null;

            return new SecureToken
            {
                TokenId = tokenId,
                Token = data.Token,
                DataHash = data.DataHash,
                CreatedAt = data.CreatedAt,
                ExpiresAt = data.ExpiresAt,
                Metadata = data.Metadata ?? new Dictionary<string, object>()
            };
        }

        public async ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var key = GetTokenKey(tokenId);
            var removed = await _database.KeyDeleteAsync(key);

            if (removed)
            {
                await _database.SetRemoveAsync($"{_keyPrefix}index", tokenId);
                _logger.LogDebug("Token removed from Redis: {TokenId}", tokenId);
            }

            return removed;
        }

        public async ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var key = GetTokenKey(tokenId);
            var json = await _database.StringGetAsync(key);

            if (!json.HasValue)
                return false;

            var data = JsonSerializer.Deserialize<TokenData>(json!, _jsonOptions);
            if (data == null)
                return false;

            data = data with { IsRevoked = true, RevokedAt = DateTimeOffset.UtcNow };
            var updatedJson = JsonSerializer.Serialize(data, _jsonOptions);

            await _database.StringSetAsync(key, updatedJson, keepTtl: true);

            _logger.LogDebug("Token revoked in Redis: {TokenId}", tokenId);
            return true;
        }

        public async ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var key = GetTokenKey(tokenId);
            return await _database.KeyExistsAsync(key);
        }

        public async ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var indexKey = $"{_keyPrefix}index";
            var tokenIds = await _database.SetMembersAsync(indexKey);

            var tokens = new List<SecureToken>();
            var currentTime = DateTimeOffset.UtcNow;

            foreach (var tokenId in tokenIds.Take(criteria.MaxResults ?? int.MaxValue).Skip(criteria.Offset))
            {
                var token = await GetTokenAsync(tokenId!, cancellationToken);
                if (token == null)
                    continue;

                // Aplicar filtros
                if (!criteria.IncludeRevoked && !token.IsValid)
                    continue;

                if (!criteria.IncludeExpired && token.IsExpired)
                    continue;

                if (criteria.CreatedAfter.HasValue && token.CreatedAt < criteria.CreatedAfter.Value)
                    continue;

                if (criteria.CreatedBefore.HasValue && token.CreatedAt > criteria.CreatedBefore.Value)
                    continue;

                if (criteria.ExpiresAfter.HasValue && token.ExpiresAt < criteria.ExpiresAfter.Value)
                    continue;

                if (criteria.ExpiresBefore.HasValue && token.ExpiresAt > criteria.ExpiresBefore.Value)
                    continue;

                tokens.Add(token);
            }

            return tokens.OrderByDescending(t => t.CreatedAt);
        }

        public async ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var indexKey = $"{_keyPrefix}index";
            var tokenIds = await _database.SetMembersAsync(indexKey);

            var expiredCount = 0;
            var currentTime = DateTimeOffset.UtcNow;

            foreach (var tokenId in tokenIds)
            {
                var key = GetTokenKey(tokenId!);
                var json = await _database.StringGetAsync(key);

                if (!json.HasValue)
                {
                    await _database.SetRemoveAsync(indexKey, tokenId);
                    expiredCount++;
                    continue;
                }

                var data = JsonSerializer.Deserialize<TokenData>(json!, _jsonOptions);
                if (data?.ExpiresAt.HasValue == true && data.ExpiresAt.Value <= currentTime)
                {
                    await _database.KeyDeleteAsync(key);
                    await _database.SetRemoveAsync(indexKey, tokenId);
                    expiredCount++;
                }
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens from Redis", expiredCount);
            }

            return expiredCount;
        }

        public async ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var indexKey = $"{_keyPrefix}index";
            var tokenIds = await _database.SetMembersAsync(indexKey);

            long totalTokens = tokenIds.Length;
            long validTokens = 0;
            long expiredTokens = 0;
            long revokedTokens = 0;
            var currentTime = DateTimeOffset.UtcNow;

            foreach (var tokenId in tokenIds)
            {
                var key = GetTokenKey(tokenId!);
                var json = await _database.StringGetAsync(key);

                if (!json.HasValue)
                    continue;

                var data = JsonSerializer.Deserialize<TokenData>(json!, _jsonOptions);
                if (data == null)
                    continue;

                if (data.IsRevoked)
                    revokedTokens++;
                else if (data.ExpiresAt.HasValue && data.ExpiresAt.Value <= currentTime)
                    expiredTokens++;
                else
                    validTokens++;
            }

            return new StorageStatistics
            {
                TotalTokens = totalTokens,
                ValidTokens = validTokens,
                ExpiredTokens = expiredTokens,
                RevokedTokens = revokedTokens,
                StorageSize = 0, // Redis não fornece isso facilmente
                LastCleanup = DateTime.UtcNow
            };
        }

        private string GetTokenKey(string tokenId) => $"{_keyPrefix}{tokenId}";

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Dados do token armazenados no Redis
        /// </summary>
        private record TokenData
        {
            public required string Token { get; init; }
            public required string DataHash { get; init; }
            public required DateTime CreatedAt { get; init; }
            public DateTime? ExpiresAt { get; init; }
            public Dictionary<string, object>? Metadata { get; init; }
            public required bool IsRevoked { get; init; }
            public DateTimeOffset? RevokedAt { get; init; }
            public required DateTimeOffset StoredAt { get; init; }
        }
    }
}
