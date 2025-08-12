using bks.sdk.Core.Cryptography;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    internal sealed class InMemoryTokenStorage : ITokenStorage
    {
        private readonly ConcurrentDictionary<string, StoredToken> _tokens;
        private readonly IBksLogger _logger;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public InMemoryTokenStorage(IBksLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokens = new ConcurrentDictionary<string, StoredToken>();

            // Configurar limpeza automática a cada hora
            _cleanupTimer = new Timer(async _ => await CleanupExpiredTokensAsync(), null,
                TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public ValueTask StoreTokenAsync(SecureToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var storedToken = new StoredToken
            {
                Token = token,
                StoredAt = DateTimeOffset.UtcNow,
                IsRevoked = false
            };

            _tokens.AddOrUpdate(token.TokenId, storedToken, (_, _) => storedToken);

            _logger.LogDebug("Token stored in memory: {TokenId}", token.TokenId);

            return ValueTask.CompletedTask;
        }

        public ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return ValueTask.FromResult<SecureToken?>(null);

            if (_tokens.TryGetValue(tokenId, out var storedToken))
            {
                // Verificar se não está revogado
                if (storedToken.IsRevoked)
                {
                    _logger.LogWarning("Attempted to retrieve revoked token: {TokenId}", tokenId);
                    return ValueTask.FromResult<SecureToken?>(null);
                }

                return ValueTask.FromResult<SecureToken?>(storedToken.Token);
            }

            return ValueTask.FromResult<SecureToken?>(null);
        }

        public ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return ValueTask.FromResult(false);

            var removed = _tokens.TryRemove(tokenId, out _);

            if (removed)
            {
                _logger.LogDebug("Token removed from memory: {TokenId}", tokenId);
            }

            return ValueTask.FromResult(removed);
        }

        public ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return ValueTask.FromResult(false);

            if (_tokens.TryGetValue(tokenId, out var storedToken))
            {
                var revokedToken = storedToken with { IsRevoked = true, RevokedAt = DateTimeOffset.UtcNow };
                _tokens.TryUpdate(tokenId, revokedToken, storedToken);

                _logger.LogDebug("Token revoked in memory: {TokenId}", tokenId);
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return ValueTask.FromResult(false);

            var exists = _tokens.ContainsKey(tokenId);
            return ValueTask.FromResult(exists);
        }

        public ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var query = _tokens.Values.AsEnumerable();

            // Aplicar filtros
            if (!criteria.IncludeRevoked)
                query = query.Where(t => !t.IsRevoked);

            if (!criteria.IncludeExpired)
                query = query.Where(t => t.Token.IsValid);

            if (criteria.CreatedAfter.HasValue)
                query = query.Where(t => t.Token.CreatedAt >= criteria.CreatedAfter.Value);

            if (criteria.CreatedBefore.HasValue)
                query = query.Where(t => t.Token.CreatedAt <= criteria.CreatedBefore.Value);

            if (criteria.ExpiresAfter.HasValue)
                query = query.Where(t => t.Token.ExpiresAt >= criteria.ExpiresAfter.Value);

            if (criteria.ExpiresBefore.HasValue)
                query = query.Where(t => t.Token.ExpiresAt <= criteria.ExpiresBefore.Value);

            // Aplicar filtros de metadados
            foreach (var filter in criteria.MetadataFilters)
            {
                query = query.Where(t => t.Token.Metadata.ContainsKey(filter.Key) &&
                                        t.Token.Metadata[filter.Key].Equals(filter.Value));
            }

            // Aplicar paginação
            query = query.Skip(criteria.Offset);

            if (criteria.MaxResults.HasValue)
                query = query.Take(criteria.MaxResults.Value);

            var results = query.Select(t => t.Token).ToList();

            return ValueTask.FromResult<IEnumerable<SecureToken>>(results);
        }

        public ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var expiredTokens = _tokens.Values
                .Where(t => t.Token.IsExpired)
                .Select(t => t.Token.TokenId)
                .ToList();

            var removedCount = 0;
            foreach (var tokenId in expiredTokens)
            {
                if (_tokens.TryRemove(tokenId, out _))
                    removedCount++;
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens from memory", removedCount);
            }

            return ValueTask.FromResult(removedCount);
        }

        public ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var allTokens = _tokens.Values.ToList();

            var statistics = new StorageStatistics
            {
                TotalTokens = allTokens.Count,
                ValidTokens = allTokens.Count(t => !t.IsRevoked && t.Token.IsValid),
                ExpiredTokens = allTokens.Count(t => t.Token.IsExpired),
                RevokedTokens = allTokens.Count(t => t.IsRevoked),
                StorageSize = EstimateStorageSize(allTokens),
                LastCleanup = DateTime.UtcNow // Em implementação real, seria rastreado
            };

            return ValueTask.FromResult(statistics);
        }

        private static long EstimateStorageSize(IEnumerable<StoredToken> tokens)
        {
            // Estimativa simples do tamanho em memória
            return tokens.Sum(t =>
                t.Token.Token.Length * sizeof(char) +
                t.Token.TokenId.Length * sizeof(char) +
                t.Token.DataHash.Length * sizeof(char) +
                256); // Overhead estimado para outros campos
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _tokens.Clear();
                _disposed = true;
            }
        }

        private record StoredToken
        {
            public required SecureToken Token { get; init; }
            public required DateTimeOffset StoredAt { get; init; }
            public required bool IsRevoked { get; init; }
            public DateTimeOffset? RevokedAt { get; init; }
        }

    }

}
