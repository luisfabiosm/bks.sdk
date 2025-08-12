using bks.sdk.Core.Enums;
using BKS.SDK.Core.Configuration;
using BKS.SDK.Core.Observability;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace bks.sdk.Core.Authentication
{
    internal sealed class ApplicationKeyValidator : IApplicationKeyValidator, IDisposable
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IBksLogger _logger;
        private readonly IMemoryCache _cache;
        private readonly BksConfiguration _options;
        private readonly ActivitySource _activitySource;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _validationSemaphores;

        private static readonly ActivitySource ActivitySource = new("BKS.SDK.Authentication");

        public ApplicationKeyValidator(
            IConfigurationProvider configurationProvider,
            IBksLogger logger,
            IMemoryCache cache,
            IOptions<BksConfiguration> options)
        {
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _activitySource = ActivitySource;
            _validationSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async ValueTask<ApplicationValidationResult> ValidateAsync(string applicationKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(applicationKey))
            {
                return ApplicationValidationResult.Invalid("Application key cannot be null or empty");
            }

            using var activity = _activitySource.StartActivity("ValidateApplicationKey");
            activity?.SetTag("application.key.hash", ComputeKeyHash(applicationKey));

            var cacheKey = $"app_validation_{ComputeKeyHash(applicationKey)}";

            // Verificar cache primeiro
            if (_cache.TryGetValue(cacheKey, out ApplicationValidationResult cachedResult))
            {
                activity?.SetTag("cache.hit", true);
                _logger.LogDebug("Application validation result found in cache");
                return cachedResult;
            }

            activity?.SetTag("cache.hit", false);

            // Usar semáforo para evitar validações paralelas da mesma chave
            var semaphore = _validationSemaphores.GetOrAdd(applicationKey, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync(cancellationToken);

                // Verificar cache novamente após obter o lock
                if (_cache.TryGetValue(cacheKey, out cachedResult))
                {
                    return cachedResult;
                }

                var result = await PerformValidationAsync(applicationKey, cancellationToken);

                // Cache o resultado
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.Security.CacheExpirationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(_options.Security.CacheSlidingExpirationMinutes),
                    Priority = CacheItemPriority.High
                };

                _cache.Set(cacheKey, result, cacheOptions);

                activity?.SetTag("validation.result", result.IsValid);

                if (result.IsValid)
                {
                    _logger.LogInformation("Application key validated successfully for {ApplicationName}",
                        result.ApplicationInfo?.ApplicationName);
                }
                else
                {
                    _logger.LogWarning("Application key validation failed: {ErrorMessage}", result.ErrorMessage);
                }

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async ValueTask<ApplicationInfo?> GetApplicationInfoAsync(string applicationKey, CancellationToken cancellationToken = default)
        {
            var validationResult = await ValidateAsync(applicationKey, cancellationToken);
            return validationResult.IsValid ? validationResult.ApplicationInfo : null;
        }

        private async ValueTask<ApplicationValidationResult> PerformValidationAsync(string applicationKey, CancellationToken cancellationToken)
        {
            try
            {
                var configKey = $"applications:{ComputeKeyHash(applicationKey)}";
                var applicationData = await _configurationProvider.GetAsync<ApplicationInfo>(configKey, cancellationToken);

                if (applicationData == null)
                {
                    return ApplicationValidationResult.Invalid("Application not found");
                }

                // Verificar se a chave corresponde
                if (!SecureStringEquals(applicationData.ApplicationKey, applicationKey))
                {
                    return ApplicationValidationResult.Invalid("Invalid application key");
                }

                // Verificar status da aplicação
                if (applicationData.Status != ApplicationStatus.Active)
                {
                    return ApplicationValidationResult.Invalid($"Application is {applicationData.Status.ToString().ToLower()}");
                }

                // Atualizar último acesso se configurado
                if (_options.Security.TrackLastAccess)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var updatedData = applicationData with { LastAccess = DateTime.UtcNow };
                            await _configurationProvider.SetAsync(configKey, updatedData, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update last access for application {ApplicationId}",
                                applicationData.ApplicationId);
                        }
                    }, CancellationToken.None);
                }

                return ApplicationValidationResult.Valid(applicationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application key validation");
                return ApplicationValidationResult.Invalid("Internal validation error");
            }
        }

        private static string ComputeKeyHash(string key)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hashBytes)[..16]; // Primeiros 16 caracteres para cache key
        }

        private static bool SecureStringEquals(string a, string b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Length != b.Length) return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        public void Dispose()
        {
            foreach (var semaphore in _validationSemaphores.Values)
            {
                semaphore.Dispose();
            }
            _validationSemaphores.Clear();
            _activitySource.Dispose();
        }
    }


}
