using bks.sdk.Core.Configuration;
using bks.sdk.Core.Cryptography;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    internal sealed class SqlServerTokenStorage : ITokenStorage
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly IBksLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public SqlServerTokenStorage(IOptions<BksConfiguration> configuration, IBksLogger logger)
        {
            var config = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            _connectionString = config.Storage.SqlServerConnectionString ??
                throw new InvalidOperationException("SQL Server connection string not configured");
            _tableName = config.Storage.TokenTableName;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            InitializeDatabase();
        }

        public async ValueTask StoreTokenAsync(SecureToken token, CancellationToken cancellationToken = default)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                INSERT INTO {_tableName} 
                (TokenId, Token, DataHash, CreatedAt, ExpiresAt, Metadata, IsRevoked, StoredAt)
                VALUES 
                (@TokenId, @Token, @DataHash, @CreatedAt, @ExpiresAt, @Metadata, @IsRevoked, @StoredAt)";

            var parameters = new
            {
                TokenId = token.TokenId,
                Token = token.Token,
                DataHash = token.DataHash,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                Metadata = JsonSerializer.Serialize(token.Metadata, _jsonOptions),
                IsRevoked = false,
                StoredAt = DateTimeOffset.UtcNow
            };

            await connection.ExecuteAsync(sql, parameters);

            _logger.LogDebug("Token stored in SQL Server: {TokenId}", token.TokenId);
        }

        public async ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                SELECT TokenId, Token, DataHash, CreatedAt, ExpiresAt, Metadata, IsRevoked
                FROM {_tableName}
                WHERE TokenId = @TokenId AND IsRevoked = 0";

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { TokenId = tokenId });

            if (row == null)
                return null;

            var metadata = string.IsNullOrEmpty(row.Metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata, _jsonOptions) ?? new Dictionary<string, object>();

            return new SecureToken
            {
                TokenId = row.TokenId,
                Token = row.Token,
                DataHash = row.DataHash,
                CreatedAt = row.CreatedAt,
                ExpiresAt = row.ExpiresAt,
                Metadata = metadata
            };
        }

        public async ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"DELETE FROM {_tableName} WHERE TokenId = @TokenId";
            var rowsAffected = await connection.ExecuteAsync(sql, new { TokenId = tokenId });

            if (rowsAffected > 0)
            {
                _logger.LogDebug("Token removed from SQL Server: {TokenId}", tokenId);
            }

            return rowsAffected > 0;
        }

        public async ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                UPDATE {_tableName} 
                SET IsRevoked = 1, RevokedAt = @RevokedAt
                WHERE TokenId = @TokenId";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                TokenId = tokenId,
                RevokedAt = DateTimeOffset.UtcNow
            });

            if (rowsAffected > 0)
            {
                _logger.LogDebug("Token revoked in SQL Server: {TokenId}", tokenId);
            }

            return rowsAffected > 0;
        }

        public async ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE TokenId = @TokenId";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { TokenId = tokenId });

            return count > 0;
        }

        public async ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!criteria.IncludeRevoked)
                conditions.Add("IsRevoked = 0");

            if (!criteria.IncludeExpired)
                conditions.Add("(ExpiresAt IS NULL OR ExpiresAt > @CurrentTime)");

            if (criteria.CreatedAfter.HasValue)
            {
                conditions.Add("CreatedAt >= @CreatedAfter");
                parameters.Add("CreatedAfter", criteria.CreatedAfter.Value);
            }

            if (criteria.CreatedBefore.HasValue)
            {
                conditions.Add("CreatedAt <= @CreatedBefore");
                parameters.Add("CreatedBefore", criteria.CreatedBefore.Value);
            }

            if (criteria.ExpiresAfter.HasValue)
            {
                conditions.Add("ExpiresAt >= @ExpiresAfter");
                parameters.Add("ExpiresAfter", criteria.ExpiresAfter.Value);
            }

            if (criteria.ExpiresBefore.HasValue)
            {
                conditions.Add("ExpiresAt <= @ExpiresBefore");
                parameters.Add("ExpiresBefore", criteria.ExpiresBefore.Value);
            }

            parameters.Add("CurrentTime", DateTimeOffset.UtcNow);

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            var sql = $@"
                SELECT TokenId, Token, DataHash, CreatedAt, ExpiresAt, Metadata
                FROM {_tableName}
                {whereClause}
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS";

            if (criteria.MaxResults.HasValue)
                sql += " FETCH NEXT @MaxResults ROWS ONLY";

            parameters.Add("Offset", criteria.Offset);
            if (criteria.MaxResults.HasValue)
                parameters.Add("MaxResults", criteria.MaxResults.Value);

            var rows = await connection.QueryAsync(sql, parameters);

            return rows.Select(row =>
            {
                var metadata = string.IsNullOrEmpty(row.Metadata)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata, _jsonOptions) ?? new Dictionary<string, object>();

                return new SecureToken
                {
                    TokenId = row.TokenId,
                    Token = row.Token,
                    DataHash = row.DataHash,
                    CreatedAt = row.CreatedAt,
                    ExpiresAt = row.ExpiresAt,
                    Metadata = metadata
                };
            }).ToList();
        }

        public async ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                DELETE FROM {_tableName}
                WHERE ExpiresAt IS NOT NULL AND ExpiresAt < @CurrentTime";

            var rowsAffected = await connection.ExecuteAsync(sql, new { CurrentTime = DateTimeOffset.UtcNow });

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens from SQL Server", rowsAffected);
            }

            return rowsAffected;
        }

        public async ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $@"
                SELECT 
                    COUNT(*) as TotalTokens,
                    COUNT(CASE WHEN IsRevoked = 0 AND (ExpiresAt IS NULL OR ExpiresAt > @CurrentTime) THEN 1 END) as ValidTokens,
                    COUNT(CASE WHEN ExpiresAt IS NOT NULL AND ExpiresAt <= @CurrentTime THEN 1 END) as ExpiredTokens,
                    COUNT(CASE WHEN IsRevoked = 1 THEN 1 END) as RevokedTokens
                FROM {_tableName}";

            var stats = await connection.QueryFirstAsync(sql, new { CurrentTime = DateTimeOffset.UtcNow });

            return new StorageStatistics
            {
                TotalTokens = stats.TotalTokens,
                ValidTokens = stats.ValidTokens,
                ExpiredTokens = stats.ExpiredTokens,
                RevokedTokens = stats.RevokedTokens,
                StorageSize = 0, // Poderia ser calculado consultando sys.dm_db_partition_stats
                LastCleanup = DateTime.UtcNow // Em implementação real, seria rastreado
            };
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var createTableSql = $@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{_tableName}' AND xtype='U')
                    CREATE TABLE {_tableName} (
                        TokenId NVARCHAR(64) PRIMARY KEY,
                        Token NVARCHAR(MAX) NOT NULL,
                        DataHash NVARCHAR(256) NOT NULL,
                        CreatedAt DATETIMEOFFSET NOT NULL,
                        ExpiresAt DATETIMEOFFSET NULL,
                        Metadata NVARCHAR(MAX) NULL,
                        IsRevoked BIT NOT NULL DEFAULT 0,
                        RevokedAt DATETIMEOFFSET NULL,
                        StoredAt DATETIMEOFFSET NOT NULL DEFAULT GETUTCDATE()
                    );
                    
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_{_tableName}_ExpiresAt')
                    CREATE INDEX IX_{_tableName}_ExpiresAt ON {_tableName} (ExpiresAt);
                    
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_{_tableName}_CreatedAt')
                    CREATE INDEX IX_{_tableName}_CreatedAt ON {_tableName} (CreatedAt);
                    
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_{_tableName}_IsRevoked')
                    CREATE INDEX IX_{_tableName}_IsRevoked ON {_tableName} (IsRevoked);";

                connection.Execute(createTableSql);

                _logger.LogDebug("SQL Server token storage initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SQL Server token storage");
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
