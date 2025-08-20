using bks.sdk.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text;
using System.Text.Json;
using bks.sdk.Authentication;
using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;

namespace bks.sdk.Transactions
{
    public class TransactionTokenService : ITransactionTokenService
    {
        private readonly ILicenseValidator _licenseValidator;
        private readonly IBKSLogger _logger;

        public TransactionTokenService(
            ILicenseValidator licenseValidator,
            IBKSLogger logger)
        {
            _licenseValidator = licenseValidator;
            _logger = logger;
        }


        public async Task<Result<TransactionTokenData>> RecoverTransactionAsync(string token)
        {
            try
            {
                _logger.Info("Iniciando recuperação de transação por token");

                // Validar token
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Result<TransactionTokenData>.Failure("Token inválido");
                }

                // Descriptografar token
                var tokenData = DecryptToken(token);
                if (tokenData == null)
                {
                    return Result<TransactionTokenData>.Failure("Falha ao descriptografar token");
                }

                // Validar integridade
                if (!ValidateTokenIntegrity(tokenData))
                {
                    return Result<TransactionTokenData>.Failure("Token com integridade comprometida");
                }

                _logger.Info($"Token da transação {tokenData.CorrelationId} recuperado com sucesso");

                return Result<TransactionTokenData>.Success(tokenData);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao recuperar transação por token: {ex.Message}");
                return Result<TransactionTokenData>.Failure("Erro interno na recuperação do token");
            }
        }


        public async Task<Result<T>> RecoverTransactionAsync<T>(string token) where T : BaseTransaction
        {
            var tokenResult = await RecoverTransactionAsync(token);

            if (!tokenResult.IsSuccess)
            {
                return Result<T>.Failure(tokenResult.Error!);
            }

            var tokenData = tokenResult.Value!;

            // Verificar tipo
            var expectedType = typeof(T).Name;
            if (tokenData.Type != expectedType)
            {
                return Result<T>.Failure($"Token é do tipo {tokenData.Type}, esperado {expectedType}");
            }

            try
            {
                // Descriptografar payload
                var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(tokenData.Payload));
                var transaction = BaseTransaction.Deserialize<T>(payloadJson);

                if (transaction == null)
                {
                    return Result<T>.Failure("Falha ao deserializar transação");
                }

                return Result<T>.Success(transaction);
            }
            catch (Exception ex)
            {
                _logger.Error($"Erro ao deserializar transação do token: {ex.Message}");
                return Result<T>.Failure("Erro na deserialização da transação");
            }
        }


        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var result = await RecoverTransactionAsync(token);
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }


        private TransactionTokenData? DecryptToken(string token)
        {
            try
            {
                // Decodificar Base64
                var encryptedBytes = Convert.FromBase64String(token);

                // Descriptografar
                var decryptedJson = BaseTransaction.DecryptData(encryptedBytes);

                // Deserializar
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return JsonSerializer.Deserialize<TransactionTokenData>(decryptedJson, options);
            }
            catch
            {
                return null;
            }
        }

 
        private bool ValidateTokenIntegrity(TransactionTokenData tokenData)
        {
            try
            {
                // Verificar campos obrigatórios
                if (string.IsNullOrWhiteSpace(tokenData.CorrelationId) ||
                    string.IsNullOrWhiteSpace(tokenData.Type) ||
                    string.IsNullOrWhiteSpace(tokenData.IntegrityHash) ||
                    string.IsNullOrWhiteSpace(tokenData.Payload))
                {
                    return false;
                }

                // Verificar idade do token (não pode ser muito antigo)
                if (DateTime.TryParse(tokenData.CreatedAt, out var createdAt))
                {
                    var maxAge = TimeSpan.FromDays(30); // Tokens válidos por 30 dias
                    if (DateTime.UtcNow - createdAt > maxAge)
                    {
                        _logger.Warn($"Token expirado: criado em {createdAt}");
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
