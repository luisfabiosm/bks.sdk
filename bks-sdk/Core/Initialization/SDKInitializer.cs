
using bks.sdk.Authentication;
using bks.sdk.Authentication.Implementations;
using bks.sdk.Cache;
using bks.sdk.Cache.Implementations;
using bks.sdk.Core.Configuration;
using bks.sdk.Enum;
using bks.sdk.Events;
using bks.sdk.Events.Implementations;
using bks.sdk.HealthCheck;
using bks.sdk.Mediator;
using bks.sdk.Observability;
using bks.sdk.Observability.Logging;
using bks.sdk.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text;

namespace bks.sdk.Core.Initialization
{

    public static class SDKInitializer
    {
    
        public static IServiceCollection AddBKSSDK(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            // 1. Carregar configurações
            var settings = LoadSettings(configuration);

            // 2. Registrar configurações como singleton
            services.AddSingleton(settings);

            // 3. Configurar logging (Serilog)
            ConfigureLogging(services, settings);

            // 4. Configurar autenticação e segurança
            ConfigureAuthentication(services, settings);

            // 5. Configurar cache distribuído
            ConfigureCache(services, settings);

            // 6. Configurar mediator pattern
            ConfigureMediator(services);

            // 7. Configurar sistema de eventos
            ConfigureEvents(services, settings);

            // 8. Configurar observabilidade (OpenTelemetry)
            ConfigureObservability(services, settings);

            // 9. Configurar processamento de transações
            ConfigureTransactions(services);

            // 10. Configurar health checks
            ConfigureHealthChecks(services, settings);

            return services;
        }

        private static SDKSettings LoadSettings(IConfiguration? configuration)
        {
            var settings = new SDKSettings();

            if (configuration != null)
            {
                // Tentar carregar da seção 'bkssdk' da configuração
                var sdkSection = configuration.GetSection("bkssdk");
                if (sdkSection.Exists())
                {
                    sdkSection.Bind(settings);
                }
                else
                {
                    // Fallback para seção raiz
                    configuration.Bind(settings);
                }
            }
            else
            {
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("sdksettings.json", optional: true, reloadOnChange: true);

                var config = configBuilder.Build();
                var sdkSection = config.GetSection("bkssdk");

                if (sdkSection.Exists())
                {
                    sdkSection.Bind(settings);
                }
            }

            // Validar configurações obrigatórias
            ValidateSettings(settings);

            return settings;
        }

        private static void ValidateSettings(SDKSettings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.LicenseKey))
            {
                errors.Add("LicenseKey é obrigatório");
            }

            if (string.IsNullOrWhiteSpace(settings.ApplicationName))
            {
                errors.Add("ApplicationName é obrigatório");
            }

            if (string.IsNullOrWhiteSpace(settings.Jwt.SecretKey))
            {
                errors.Add("Jwt.SecretKey é obrigatório");
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Configurações inválidas do sdk: {string.Join(", ", errors)}");
            }
        }

    
        private static void ConfigureLogging(IServiceCollection services, SDKSettings settings)
        {
            // Configurar Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", settings.ApplicationName)
                .Enrich.WithProperty("ServiceName", settings.Observability.ServiceName)
                .Enrich.WithProperty("ServiceVersion", settings.Observability.ServiceVersion)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/bks-sdk-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Registrar Serilog como provider de logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            // Registrar logger customizado do SDK
            services.AddSingleton<IBKSLogger, SerilogLogger>();
        }


        private static void ConfigureAuthentication(IServiceCollection services, SDKSettings settings)
        {
            // Registrar serviços de autenticação
            services.AddSingleton<ILicenseValidator, LicenseValidator>();
            services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
            services.AddSingleton<ITransactionTokenService, TransactionTokenService>();

            // Configurar autenticação JWT
            var key = Encoding.UTF8.GetBytes(settings.Jwt.SecretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Para desenvolvimento
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Jwt.Issuer,
                    ValidAudience = settings.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization();
        }


        private static void ConfigureCache(IServiceCollection services, SDKSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.Redis.ConnectionString))
            {
                services.AddSingleton<ICacheProvider>(provider =>
                    new RedisCacheProvider(settings.Redis.ConnectionString));
            }
            else
            {
                // Cache em memória como fallback
                services.AddMemoryCache();
                services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
            }
        }

        private static void ConfigureMediator(IServiceCollection services)
        {
            services.AddSingleton<IMediator, Mediator.Implementations.Mediator>();

            // Registrar handlers automaticamente via reflection
            RegisterMediatorHandlers(services);
        }

   
        private static void RegisterMediatorHandlers(IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var handlerTypes = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                    .Where(t => !t.IsAbstract && !t.IsInterface);

                foreach (var handlerType in handlerTypes)
                {
                    var interfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

                    foreach (var @interface in interfaces)
                    {
                        services.AddTransient(@interface, handlerType);
                    }
                }
            }
        }

   
        private static void ConfigureEvents(IServiceCollection services, SDKSettings settings)
        {
            // Registrar dispatcher de eventos
            services.AddSingleton<DomainEventDispatcher>();

            // Configurar broker baseado no tipo especificado
            switch (settings.EventBroker.BrokerType)
            {
                case EventBrokerType.RabbitMQ:
                    // TODO: Implementar RabbitMQEventBroker
                    services.AddSingleton<IEventBroker, InMemoryEventBroker>();
                    break;

                case EventBrokerType.Kafka:
                    // TODO: Implementar KafkaEventBroker
                    services.AddSingleton<IEventBroker, InMemoryEventBroker>();
                    break;

                case EventBrokerType.GooglePubSub:
                    // TODO: Implementar GooglePubSubEventBroker
                    services.AddSingleton<IEventBroker, InMemoryEventBroker>();
                    break;

                default:
                    // Fallback para implementação em memória
                    services.AddSingleton<IEventBroker, InMemoryEventBroker>();
                    break;
            }
        }

        private static void ConfigureObservability(IServiceCollection services, SDKSettings settings)
        {
            // Configurar OpenTelemetry
            services.AddBKSObservability(settings.Observability);
        }

      
        private static void ConfigureTransactions(IServiceCollection services)
        {
            services.AddScoped<ITransactionTokenService, TransactionTokenService>();
        }

    
        private static void ConfigureHealthChecks(IServiceCollection services, SDKSettings settings)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // Health Check básico do SDK
            healthChecksBuilder.AddCheck<SdkHealthCheck>("bks-sdk",
                tags: new[] { "sdk", "ready", "live" });

            // Health Check do Redis (se configurado)
            if (!string.IsNullOrWhiteSpace(settings.Redis.ConnectionString))
            {
                healthChecksBuilder.AddCheck<RedisHealthCheck>("redis",
                    tags: new[] { "external", "cache", "ready" });
            }

            // Health Check do sistema de eventos
            healthChecksBuilder.AddCheck<EventBrokerHealthCheck>("event-broker",
                tags: new[] { "external", "messaging", "ready" });

            // Health Check do JWT (validação de configuração)
            healthChecksBuilder.AddCheck<JwtHealthCheck>("jwt-config",
                tags: new[] { "config", "security", "ready" });

        }
    }
}



