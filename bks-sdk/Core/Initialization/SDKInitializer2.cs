using bks.sdk.Authentication;
using bks.sdk.Authentication.Implementations;
using bks.sdk.Cache;
using bks.sdk.Cache.Implementations;
using bks.sdk.Core.Configuration;
using bks.sdk.Core.Middlewares;
using bks.sdk.Enum;
using bks.sdk.Events;
using bks.sdk.Events.Implementations;
using bks.sdk.HealthCheck;
using bks.sdk.Mediator;
using bks.sdk.Observability;
using bks.sdk.Observability.Logging;
using bks.sdk.Observability.Tracing;
using bks.sdk.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text;

namespace bks.sdk.Core.Initialization;


public static class SDKInitializer2
{

    public static IServiceCollection AddBKSSDK2(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // 1. Carregar configurações
        var settings = LoadSDKSettings(configuration);
        services.AddSingleton(settings);

        // 2. Validar licença
        ValidateLicense(settings);

        // 3. Configurar serviços core
        ConfigureCoreServices(services, settings);

        // 4. Configurar autenticação
        ConfigureAuthentication(services, settings);

        // 5. Configurar cache
        ConfigureCache(services, settings);

        // 6. Configurar eventos
        ConfigureEventBroker(services, settings);

        // 7. Configurar mediator
        ConfigureMediator(services);

        // 8. Configurar observabilidade
        ConfigureObservability(services, settings);

        // 9. Configurar transações
        ConfigureTransactions(services);

        // 10. Configurar health checks
        ConfigureHealthChecks(services, settings);


        return services;
    }


    public static WebApplication UseBksSdk(this WebApplication app)
    {
        // 1. Middleware de autenticação
        app.UseAuthentication();
        app.UseAuthorization();

        // 2. Middleware de observabilidade (já configurado pelo OpenTelemetry)

        // 3. Middleware de correlação de transações
        app.UseMiddleware<TransactionCorrelationMiddleware>();

        // 4. Middleware de logging de requisições
        app.UseMiddleware<RequestLoggingMiddleware>();

        return app;
    }

    #region Métodos Privados de Configuração


    private static SDKSettings LoadSDKSettings(IConfiguration? configuration)
    {
        var settings = new SDKSettings();

        if (configuration != null)
        {
            // Tentar carregar do appsettings.json
            configuration.GetSection("bkssdk").Bind(settings);
        }

        // Se não encontrou configuração ou está incompleta, tentar arquivo dedicado
        if (string.IsNullOrEmpty(settings.LicenseKey) || string.IsNullOrEmpty(settings.ApplicationName))
        {
            try
            {
                Console.WriteLine(Directory.GetCurrentDirectory());

                var sdkConfigBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("sdksettings.json", optional: true, reloadOnChange: true);

                var sdkConfig = sdkConfigBuilder.Build();
                sdkConfig.GetSection("bkssdk").Bind(settings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Não foi possível carregar as configurações do bks.sdk. " +
                    "Verifique se existe um arquivo 'sdksettings.json' ou configure a seção 'bkssdk' no appsettings.json",
                    ex);
            }
        }

        return settings;
    }


    private static void ValidateLicense(SDKSettings settings)
    {
      
        if (string.IsNullOrWhiteSpace(settings.LicenseKey))
        {
            throw new InvalidOperationException("LicenseKey do SDK não configurada");
        }

        if (string.IsNullOrWhiteSpace(settings.ApplicationName))
        {
            throw new InvalidOperationException("ApplicationName do sdk configurado");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
        {
            throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT do sdk não configurado");
        }


        var validator = new LicenseValidator(settings);

        // Validação básica da licença (em produção seria mais complexa)
        if ((Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"))
        {
            if (!validator.ValidateDev(settings.LicenseKey))
            {
                throw new UnauthorizedAccessException("Licença do sdk inválida");
            }
            return;
        }

        //Validacao Completa em produção
        if (!validator.Validate(settings.LicenseKey, settings.ApplicationName))
        {
            throw new UnauthorizedAccessException("Licença do sdk inválida");
        }

      
    }

    private static void ConfigureHealthChecks(IServiceCollection services, SDKSettings settings)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Health Check básico do SDK
        healthChecksBuilder.AddCheck<SdkHealthCheck>("bks-sdk",
            tags: new[] { "sdk", "ready" });

        // Health Check do Redis (se configurado)
        if (!string.IsNullOrWhiteSpace(settings.Redis.ConnectionString))
        {
            healthChecksBuilder.AddCheck<RedisHealthCheck>("redis",
                tags: new[] { "external", "cache" });
        }

        // Health Check do sistema de eventos
        healthChecksBuilder.AddCheck<EventBrokerHealthCheck>("event-broker",
            tags: new[] { "external", "messaging" });

        // Health Check do JWT (validação de configuração)
        healthChecksBuilder.AddCheck<JwtHealthCheck>("jwt-config",
            tags: new[] { "config", "security" });
    }
    private static void ConfigureCoreServices(IServiceCollection services, SDKSettings settings)
    {
        // Logging
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/bks-sdk-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Logger = logger;
        services.AddSingleton<bks.sdk.Observability.Logging.IBKSLogger, SerilogLogger>();
    }


    private static void ConfigureAuthentication(IServiceCollection services, SDKSettings settings)
    {
        // Registrar serviços de autenticação
        services.AddScoped<ILicenseValidator, LicenseValidator>();
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

        // Configurar autenticação JWT do ASP.NET
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Jwt.Issuer,
                    ValidAudience = settings.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.Jwt.SecretKey))
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

 
    private static void ConfigureEventBroker(IServiceCollection services, SDKSettings settings)
    {
        services.AddSingleton<DomainEventDispatcher>();

        // Registrar broker baseado na configuração
        switch (settings.EventBroker.BrokerType)
        {
            case EventBrokerType.RabbitMQ:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.IBKSLogger>();
                    return new RabbitMqEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            case EventBrokerType.Kafka:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.IBKSLogger>();
                    return new KafkaEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            case EventBrokerType.GooglePubSub:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.IBKSLogger>();
                    return new GooglePubSubEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            default:
                // Broker em memória para desenvolvimento
                services.AddSingleton<IEventBroker, InMemoryEventBroker>();
                break;
        }
    }


    private static void ConfigureMediator(IServiceCollection services)
    {
        services.AddScoped<IMediator, bks.sdk.Mediator.Implementations.Mediator>();
    }

    private static void ConfigureObservability(IServiceCollection services, SDKSettings settings)
    {
        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", settings.ApplicationName)
            .Enrich.WithProperty("ServiceName", settings.Observability.ServiceName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/bks-sdk-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // Registrar logger customizado do SDK
        services.AddSingleton<IBKSLogger, SerilogLogger>();

        // Configurar OpenTelemetry
        services.AddBKSObservability(settings.Observability);
    }


    private static void ConfigureTransactions(IServiceCollection services)
    {
        services.AddScoped<ITransactionTokenService, TransactionTokenService>();
    }

    #endregion
}




