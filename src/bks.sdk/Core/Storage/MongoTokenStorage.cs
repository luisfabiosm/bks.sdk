using bks.sdk.Core.Configuration;
using bks.sdk.Core.Cryptography;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    internal sealed class MongoTokenStorage : ITokenStorage
    {
        private readonly IMongoCollection<TokenDocument> _collection;
        private readonly IBksLogger _logger;
        private bool _disposed;

        public MongoTokenStorage(IOptions<BksConfiguration> configuration, IBksLogger logger)
        {
            var config = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            var connectionString = config.Storage.MongoConnectionString ??
                throw new InvalidOperationException("MongoDB connection string not configured");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(config.Storage.DatabaseName);
            _collection = database.GetCollection<TokenDocument>(config.Storage.TokenTableName);

            CreateIndexes();
        }

        public async ValueTask StoreTokenAsync(SecureToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            var document = new TokenDocument
            {
                TokenId = token.TokenId,
                Token = token.Token,
                DataHash = token.DataHash,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                Metadata = token.Metadata,
                IsRevoked = false,
                StoredAt = DateTimeOffset.UtcNow
            };

            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);

            _logger.LogDebug("Token stored in MongoDB: {TokenId}", token.TokenId);
        }

        public async ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return null;

            var filter = Builders<TokenDocument>.Filter.And(
                Builders<TokenDocument>.Filter.Eq(d => d.TokenId, tokenId),
                Builders<TokenDocument>.Filter.Eq(d => d.IsRevoked, false)
            );

            var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (document == null)
                return null;

            return new SecureToken
            {
                TokenId = document.TokenId,
                Token = document.Token,
                DataHash = document.DataHash,
                CreatedAt = document.CreatedAt,
                ExpiresAt = document.ExpiresAt,
                Metadata = document.Metadata ?? new Dictionary<string, object>()
            };
        }

        public async ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var filter = Builders<TokenDocument>.Filter.Eq(d => d.TokenId, tokenId);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);

            if (result.DeletedCount > 0)
            {
                _logger.LogDebug("Token removed from MongoDB: {TokenId}", tokenId);
            }

            return result.DeletedCount > 0;
        }

        public async ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var filter = Builders<TokenDocument>.Filter.Eq(d => d.TokenId, tokenId);
            var update = Builders<TokenDocument>.Update
                .Set(d => d.IsRevoked, true)
                .Set(d => d.RevokedAt, DateTimeOffset.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
            {
                _logger.LogDebug("Token revoked in MongoDB: {TokenId}", tokenId);
            }

            return result.ModifiedCount > 0;
        }

        public async ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            var filter = Builders<TokenDocument>.Filter.Eq(d => d.TokenId, tokenId);
            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            return count > 0;
        }

        public async ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var filterBuilder = Builders<TokenDocument>.Filter;
            var filters = new List<FilterDefinition<TokenDocument>>();

            if (!criteria.IncludeRevoked)
                filters.Add(filterBuilder.Eq(d => d.IsRevoked, false));

            if (!criteria.IncludeExpired)
            {
                filters.Add(filterBuilder.Or(
                    filterBuilder.Eq(d => d.ExpiresAt, null),
                    filterBuilder.Gt(d => d.ExpiresAt, DateTimeOffset.UtcNow)
                ));
            }

            if (criteria.CreatedAfter.HasValue)
                filters.Add(filterBuilder.Gte(d => d.CreatedAt, criteria.CreatedAfter.Value));

            if (criteria.CreatedBefore.HasValue)
                filters.Add(filterBuilder.Lte(d => d.CreatedAt, criteria.CreatedBefore.Value));

            if (criteria.ExpiresAfter.HasValue)
                filters.Add(filterBuilder.Gte(d => d.ExpiresAt, criteria.ExpiresAfter.Value));

            if (criteria.ExpiresBefore.HasValue)
                filters.Add(filterBuilder.Lte(d => d.ExpiresAt, criteria.ExpiresBefore.Value));

            var filter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

            var query = _collection.Find(filter)
                .Sort(Builders<TokenDocument>.Sort.Descending(d => d.CreatedAt))
                .Skip(criteria.Offset);

            if (criteria.MaxResults.HasValue)
                query = query.Limit(criteria.MaxResults.Value);

            var documents = await query.ToListAsync(cancellationToken);

            return documents.Select(doc => new SecureToken
            {
                TokenId = doc.TokenId,
                Token = doc.Token,
                DataHash = doc.DataHash,
                CreatedAt = doc.CreatedAt,
                ExpiresAt = doc.ExpiresAt,
                Metadata = doc.Metadata ?? new Dictionary<string, object>()
            }).ToList();
        }

        public async ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var filter = Builders<TokenDocument>.Filter.And(
                Builders<TokenDocument>.Filter.Ne(d => d.ExpiresAt, null),
                Builders<TokenDocument>.Filter.Lt(d => d.ExpiresAt, DateTimeOffset.UtcNow)
            );

            var result = await _collection.DeleteManyAsync(filter, cancellationToken);

            if (result.DeletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens from MongoDB", result.DeletedCount);
            }

            return (int)result.DeletedCount;
        }

        public async ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "totalTokens", new BsonDocument("$sum", 1) },
                    { "validTokens", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$and", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray { "$isRevoked", false }),
                                new BsonDocument("$or", new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { "$expiresAt", BsonNull.Value }),
                                    new BsonDocument("$gt", new BsonArray { "$expiresAt", DateTimeOffset.UtcNow })
                                })
                            }),
                            1,
                            0
                        })) },
                    { "expiredTokens", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$and", new BsonArray
                            {
                                new BsonDocument("$ne", new BsonArray { "$expiresAt", BsonNull.Value }),
                                new BsonDocument("$lte", new BsonArray { "$expiresAt", DateTimeOffset.UtcNow })
                            }),
                            1,
                            0
                        })) },
                    { "revokedTokens", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$isRevoked", true }),
                            1,
                            0
                        })) }
                })
            };

            var aggregateResult = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);

            if (aggregateResult == null)
            {
                return new StorageStatistics
                {
                    TotalTokens = 0,
                    ValidTokens = 0,
                    ExpiredTokens = 0,
                    RevokedTokens = 0,
                    StorageSize = 0,
                    LastCleanup = DateTime.UtcNow
                };
            }

            return new StorageStatistics
            {
                TotalTokens = aggregateResult.GetValue("totalTokens", 0).AsInt64,
                ValidTokens = aggregateResult.GetValue("validTokens", 0).AsInt64,
                ExpiredTokens = aggregateResult.GetValue("expiredTokens", 0).AsInt64,
                RevokedTokens = aggregateResult.GetValue("revokedTokens", 0).AsInt64,
                StorageSize = 0, // MongoDB não fornece isso facilmente
                LastCleanup = DateTime.UtcNow
            };
        }

        private void CreateIndexes()
        {
            try
            {
                var indexes = new[]
                {
                    new CreateIndexModel<TokenDocument>(
                        Builders<TokenDocument>.IndexKeys.Ascending(d => d.TokenId),
                        new CreateIndexOptions { Unique = true }),
                    new CreateIndexModel<TokenDocument>(
                        Builders<TokenDocument>.IndexKeys.Ascending(d => d.ExpiresAt)),
                    new CreateIndexModel<TokenDocument>(
                        Builders<TokenDocument>.IndexKeys.Ascending(d => d.CreatedAt)),
                    new CreateIndexModel<TokenDocument>(
                        Builders<TokenDocument>.IndexKeys.Ascending(d => d.IsRevoked))
                };

                _collection.Indexes.CreateMany(indexes);

                _logger.LogDebug("MongoDB token storage indexes created");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create MongoDB indexes");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Documento MongoDB para tokens
        /// </summary>
        private class TokenDocument
        {
            public ObjectId Id { get; set; }
            public required string TokenId { get; set; }
            public required string Token { get; set; }
            public required string DataHash { get; set; }
            public required DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public Dictionary<string, object>? Metadata { get; set; }
            public required bool IsRevoked { get; set; }
            public DateTimeOffset? RevokedAt { get; set; }
            public required DateTimeOffset StoredAt { get; set; }
        }
    }
}
