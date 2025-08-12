using bks.sdk.Core.Configuration;
using BKS.SDK.Core.Configuration;
using BKS.SDK.Core.Observability;
using BKS.SDK.Core.Storage;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    internal sealed class SecureTokenGenerator : ISecureTokenGenerator, IDisposable
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly ITokenStorage _tokenStorage;
        private readonly IBksLogger _logger;
        private readonly BksConfiguration _configuration;
        private readonly byte[] _masterKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public SecureTokenGenerator(
            ICryptographyService cryptographyService,
            ITokenStorage tokenStorage,
            IBksLogger logger,
            IOptions<BksConfiguration> configuration)
        {
            _cryptographyService = cryptographyService ?? throw new ArgumentNullException(nameof(cryptographyService));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));

            _masterKey = DeriveMasterKey();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async ValueTask<SecureToken> GenerateTokenAsync<T>(T data, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var tokenId = Guid.NewGuid().ToString("N");
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Serializar dados
                var jsonData = JsonSerializer.Serialize(data, _jsonOptions);
                var dataBytes = Encoding.UTF8.GetBytes(jsonData);

                // Criar payload com metadados
                var payload = new TokenPayload
                {
                    TokenId = tokenId,
                    Data = jsonData,
                    CreatedAt = timestamp,
                    ExpiresAt = expiration.HasValue ? timestamp + (long)expiration.Value.TotalSeconds : null,
                    DataType = typeof(T).FullName!
                };

                var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
                var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

                // Criptografar payload
                var encryptionResult = _cryptographyService.Encrypt(payloadBytes);

                // Criar estrutura do token
                var tokenStructure = new EncryptedTokenStructure
                {
                    Version = 1,
                    TokenId = tokenId,
                    EncryptedData = Convert.ToBase64String(encryptionResult.EncryptedData),
                    Nonce = Convert.ToBase64String(encryptionResult.Nonce),
                    Tag = Convert.ToBase64String(encryptionResult.Tag),
                    Algorithm = "AES-256-GCM"
                };

                var tokenJson = JsonSerializer.Serialize(tokenStructure, _jsonOptions);
                var tokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson));

                // Calcular hash dos dados originais
                var dataHash = _cryptographyService.ComputeHash(dataBytes, Encoding.UTF8.GetBytes(tokenId));

                var secureToken = new SecureToken
                {
                    Token = tokenBase64,
                    TokenId = tokenId,
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                    ExpiresAt = payload.ExpiresAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAt.Value).DateTime : null,
                    DataHash = dataHash,
                    Metadata = new Dictionary<string, object>
                    {
                        ["DataType"] = typeof(T).FullName!,
                        ["Algorithm"] = "AES-256-GCM",
                        ["Version"] = 1
                    }
                };

                // Armazenar token
                await _tokenStorage.StoreTokenAsync(secureToken, cancellationToken);

                _logger.LogInformation("Secure token generated successfully: {TokenId}", tokenId);

                return secureToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate secure token");
                throw;
            }
        }

        public async ValueTask<T?> RetrieveDataAsync<T>(string token, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                // Verificar se o token existe no armazenamento
                var storedToken = await _tokenStorage.GetTokenAsync(ExtractTokenId(token), cancellationToken);
                if (storedToken == null || !storedToken.IsValid)
                {
                    _logger.LogWarning("Token not found or expired: {Token}", token[..Math.Min(token.Length, 10)]);
                    return null;
                }

                // Decodificar token
                var tokenBytes = Convert.FromBase64String(token);
                var tokenJson = Encoding.UTF8.GetString(tokenBytes);
                var tokenStructure = JsonSerializer.Deserialize<EncryptedTokenStructure>(tokenJson, _jsonOptions);

                if (tokenStructure == null)
                {
                    _logger.LogWarning("Invalid token structure");
                    return null;
                }

                // Descriptografar payload
                var encryptedData = Convert.FromBase64String(tokenStructure.EncryptedData);
                var nonce = Convert.FromBase64String(tokenStructure.Nonce);
                var tag = Convert.FromBase64String(tokenStructure.Tag);

                var decryptedBytes = _cryptographyService.Decrypt(encryptedData, _masterKey, nonce, tag);
                if (decryptedBytes == null)
                {
                    _logger.LogWarning("Failed to decrypt token data");
                    return null;
                }

                var payloadJson = Encoding.UTF8.GetString(decryptedBytes);
                var payload = JsonSerializer.Deserialize<TokenPayload>(payloadJson, _jsonOptions);

                if (payload == null)
                {
                    _logger.LogWarning("Invalid payload structure");
                    return null;
                }

                // Verificar expiração
                if (payload.ExpiresAt.HasValue && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAt.Value)
                {
                    _logger.LogWarning("Token expired: {TokenId}", payload.TokenId);
                    return null;
                }

                // Deserializar dados originais
                var data = JsonSerializer.Deserialize<T>(payload.Data, _jsonOptions);

                _logger.LogDebug("Token data retrieved successfully: {TokenId}", payload.TokenId);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve token data");
                return null;
            }
        }

        public async ValueTask<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenId = ExtractTokenId(token);
                var storedToken = await _tokenStorage.GetTokenAsync(tokenId, cancellationToken);

                return storedToken?.IsValid ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token");
                return false;
            }
        }

        public async ValueTask<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenId = ExtractTokenId(token);
                var result = await _tokenStorage.RevokeTokenAsync(tokenId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Token revoked successfully: {TokenId}", tokenId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke token");
                return false;
            }
        }

        private string ExtractTokenId(string token)
        {
            try
            {
                var tokenBytes = Convert.FromBase64String(token);
                var tokenJson = Encoding.UTF8.GetString(tokenBytes);
                var tokenStructure = JsonSerializer.Deserialize<EncryptedTokenStructure>(tokenJson, _jsonOptions);
                return tokenStructure?.TokenId ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private byte[] DeriveMasterKey()
        {
            var encryptionKey = _configuration.Security.EncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey))
            {
                throw new InvalidOperationException("Encryption key not configured");
            }

            var salt = Encoding.UTF8.GetBytes(_configuration.Security.HashSalt);
            var password = Encoding.UTF8.GetBytes(encryptionKey);

            return _cryptographyService.DeriveKey(password, salt, _configuration.Security.HashIterations, 32);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cryptographyService?.Dispose();
                _disposed = true;
            }
        }
    }


}
