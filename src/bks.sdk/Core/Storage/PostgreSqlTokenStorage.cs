using bks.sdk.Core.Configuration;
using bks.sdk.Core.Cryptography;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage;

internal sealed class PostgreSqlTokenStorage : ITokenStorage
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly IBksLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public PostgreSqlTokenStorage(IOptions<BksConfiguration> configuration, IBksLogger logger)
    {
        var config = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _connectionString = config.Storage.PostgreSqlConnectionString ??
            throw new InvalidOperationException("PostgreSQL connection string not configured");
        _tableName = config.Storage.TokenTableName.ToLowerInvariant();
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

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"
                INSERT INTO {_tableName} 
                (token_id, token, data_hash, created_at, expires_at, metadata, is_revoked, stored_at)
                VALUES 
                (@TokenId, @Token, @DataHash, @CreatedAt, @ExpiresAt, @Metadata::jsonb, @IsRevoked, @StoredAt)";

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

        _logger.LogDebug("Token stored in PostgreSQL: {TokenId}", token.TokenId);
    }

    public async ValueTask<SecureToken?> GetTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return null;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"
                SELECT token_id, token, data_hash, created_at, expires_at, metadata, is_revoked
                FROM {_tableName}
                WHERE token_id = @TokenId AND is_revoked = false";

        var row = await connection.QueryFirstOrDefaultAsync(sql, new { TokenId = tokenId });

        if (row == null)
            return null;

        var metadata = string.IsNullOrEmpty(row.metadata)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(row.metadata, _jsonOptions) ?? new Dictionary<string, object>();

        return new SecureToken
        {
            TokenId = row.token_id,
            Token = row.token,
            DataHash = row.data_hash,
            CreatedAt = row.created_at,
            ExpiresAt = row.expires_at,
            Metadata = metadata
        };
    }

    public async ValueTask<bool> RemoveTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return false;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"DELETE FROM {_tableName} WHERE token_id = @TokenId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { TokenId = tokenId });

        if (rowsAffected > 0)
        {
            _logger.LogDebug("Token removed from PostgreSQL: {TokenId}", tokenId);
        }

        return rowsAffected > 0;
    }

    public async ValueTask<bool> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return false;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"
                UPDATE {_tableName} 
                SET is_revoked = true, revoked_at = @RevokedAt
                WHERE token_id = @TokenId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            TokenId = tokenId,
            RevokedAt = DateTimeOffset.UtcNow
        });

        if (rowsAffected > 0)
        {
            _logger.LogDebug("Token revoked in PostgreSQL: {TokenId}", tokenId);
        }

        return rowsAffected > 0;
    }

    public async ValueTask<bool> ExistsAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return false;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE token_id = @TokenId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TokenId = tokenId });

        return count > 0;
    }

    public async ValueTask<IEnumerable<SecureToken>> ListTokensAsync(TokenSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!criteria.IncludeRevoked)
            conditions.Add("is_revoked = false");

        if (!criteria.IncludeExpired)
            conditions.Add("(expires_at IS NULL OR expires_at > @CurrentTime)");

        if (criteria.CreatedAfter.HasValue)
        {
            conditions.Add("created_at >= @CreatedAfter");
            parameters.Add("CreatedAfter", criteria.CreatedAfter.Value);
        }

        parameters.Add("CurrentTime", DateTimeOffset.UtcNow);

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $@"
                SELECT token_id, token, data_hash, created_at, expires_at, metadata
                FROM {_tableName}
                {whereClause}
                ORDER BY created_at DESC
                OFFSET @Offset";

        if (criteria.MaxResults.HasValue)
            sql += " LIMIT @MaxResults";

        parameters.Add("Offset", criteria.Offset);
        if (criteria.MaxResults.HasValue)
            parameters.Add("MaxResults", criteria.MaxResults.Value);

        var rows = await connection.QueryAsync(sql, parameters);

        return rows.Select(row =>
        {
            var metadata = string.IsNullOrEmpty(row.metadata)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(row.metadata, _jsonOptions) ?? new Dictionary<string, object>();

            return new SecureToken
            {
                TokenId = row.token_id,
                Token = row.token,
                DataHash = row.data_hash,
                CreatedAt = row.created_at,
                ExpiresAt = row.expires_at,
                Metadata = metadata
            };
        }).ToList();
    }

    public async ValueTask<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"
                DELETE FROM {_tableName}
                WHERE expires_at IS NOT NULL AND expires_at < @CurrentTime";

        var rowsAffected = await connection.ExecuteAsync(sql, new { CurrentTime = DateTimeOffset.UtcNow });

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired tokens from PostgreSQL", rowsAffected);
        }

        return rowsAffected;
    }

    public async ValueTask<StorageStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"
                SELECT 
                    COUNT(*) as total_tokens,
                    COUNT(CASE WHEN is_revoked = false AND (expires_at IS NULL OR expires_at > @CurrentTime) THEN 1 END) as valid_tokens,
                    COUNT(CASE WHEN expires_at IS NOT NULL AND expires_at <= @CurrentTime THEN 1 END) as expired_tokens,
                    COUNT(CASE WHEN is_revoked = true THEN 1 END) as revoked_tokens
                FROM {_tableName}";

        var stats = await connection.QueryFirstAsync(sql, new { CurrentTime = DateTimeOffset.UtcNow });

        return new StorageStatistics
        {
            TotalTokens = stats.total_tokens,
            ValidTokens = stats.valid_tokens,
            ExpiredTokens = stats.expired_tokens,
            RevokedTokens = stats.revoked_tokens,
            StorageSize = 0,
            LastCleanup = DateTime.UtcNow
        };
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var createTableSql = $@"
                    CREATE TABLE IF NOT EXISTS {_tableName} (
                        token_id VARCHAR(64) PRIMARY KEY,
                        token TEXT NOT NULL,
                        data_hash VARCHAR(256) NOT NULL,
                        created_at TIMESTAMPTZ NOT NULL,
                        expires_at TIMESTAMPTZ NULL,
                        metadata JSONB NULL,
                        is_revoked BOOLEAN NOT NULL DEFAULT false,
                        revoked_at TIMESTAMPTZ NULL,
                        stored_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_{_tableName}_expires_at ON {_tableName} (expires_at);
                    CREATE INDEX IF NOT EXISTS idx_{_tableName}_created_at ON {_tableName} (created_at);
                    CREATE INDEX IF NOT EXISTS idx_{_tableName}_is_revoked ON {_tableName} (is_revoked);";

            connection.Execute(createTableSql);

            _logger.LogDebug("PostgreSQL token storage initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PostgreSQL token storage");
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

