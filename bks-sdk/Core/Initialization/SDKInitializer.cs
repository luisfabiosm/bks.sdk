using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using bks.sdk.Core.Configuration;
using bks.sdk.Authentication;
using bks.sdk.Authentication.Implementations;
using bks.sdk.Cache;
using bks.sdk.Cache.Implementations;
using bks.sdk.Events;
using bks.sdk.Mediator;
using bks.sdk.Observability.Tracing;
using bks.sdk.Transactions;
using bks.sdk.Core.Middlewares;
using bks.sdk.Events.Implementations;
using bks.sdk.Enum;

namespace bks.sdk.Core.Initialization;


public static class SDKInitializer
{

    public static IServiceCollection AddBKSSDK(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // 1. Carregar configura��es
        var settings = LoadSDKSettings(configuration);
        services.AddSingleton(settings);

        // 2. Validar licen�a
        ValidateLicense(settings);

        // 3. Configurar servi�os core
        ConfigureCoreServices(services, settings);

        // 4. Configurar autentica��o
        ConfigureAuthentication(services, settings);

        // 5. Configurar cache
        ConfigureCache(services, settings);

        // 6. Configurar eventos
        ConfigureEventBroker(services, settings);

        // 7. Configurar mediator
        ConfigureMediator(services);

        // 8. Configurar observabilidade
        ConfigureObservability(services, settings);

        // 9. Configurar transa��es
        ConfigureTransactions(services);

        return services;
    }


    public static WebApplication UseBksSdk(this WebApplication app)
    {
        // 1. Middleware de autentica��o
        app.UseAuthentication();
        app.UseAuthorization();

        // 2. Middleware de observabilidade (j� configurado pelo OpenTelemetry)

        // 3. Middleware de correla��o de transa��es
        app.UseMiddleware<TransactionCorrelationMiddleware>();

        // 4. Middleware de logging de requisi��es
        app.UseMiddleware<RequestLoggingMiddleware>();

        return app;
    }

    #region M�todos Privados de Configura��o


    private static SDKSettings LoadSDKSettings(IConfiguration? configuration)
    {
        var settings = new SDKSettings();

        if (configuration != null)
        {
            // Tentar carregar do appsettings.json
            configuration.GetSection("bkssdk").Bind(settings);
        }

        // Se n�o encontrou configura��o ou est� incompleta, tentar arquivo dedicado
        if (string.IsNullOrEmpty(settings.LicenseKey) || string.IsNullOrEmpty(settings.ApplicationName))
        {
            try
            {
                var sdkConfigBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("sdksettings.json", optional: true, reloadOnChange: true);

                var sdkConfig = sdkConfigBuilder.Build();
                sdkConfig.Bind(settings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "N�o foi poss�vel carregar as configura��es do BKS.SDK. " +
                    "Verifique se existe um arquivo 'sdksettings.json' ou configure a se��o 'BKSSDK' no appsettings.json",
                    ex);
            }
        }

        return settings;
    }


    private static void ValidateLicense(SDKSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.LicenseKey))
        {
            throw new InvalidOperationException("LicenseKey do BKS.SDK n�o configurada");
        }

        if (string.IsNullOrWhiteSpace(settings.ApplicationName))
        {
            throw new InvalidOperationException("ApplicationName do BKS.SDK n�o configurado");
        }

        // Valida��o b�sica da licen�a (em produ��o seria mais complexa)
        var validator = new LicenseValidator(settings);
        if (!validator.Validate(settings.LicenseKey, settings.ApplicationName))
        {
            throw new UnauthorizedAccessException("Licen�a do BKS.SDK inv�lida");
        }
    }


    private static void ConfigureCoreServices(IServiceCollection services, SDKSettings settings)
    {
        // Logging
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/bks-sdk-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Logger = logger;
        services.AddSingleton<bks.sdk.Observability.Logging.ILogger, SerilogLogger>();
    }


    private static void ConfigureAuthentication(IServiceCollection services, SDKSettings settings)
    {
        // Registrar servi�os de autentica��o
        services.AddScoped<ILicenseValidator, LicenseValidator>();
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

        // Configurar autentica��o JWT do ASP.NET
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
            // Cache em mem�ria como fallback
            services.AddMemoryCache();
            services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        }
    }

 
    private static void ConfigureEventBroker(IServiceCollection services, SDKSettings settings)
    {
        services.AddSingleton<DomainEventDispatcher>();

        // Registrar broker baseado na configura��o
        switch (settings.EventBroker.BrokerType)
        {
            case EventBrokerType.RabbitMQ:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.ILogger>();
                    return new RabbitMqEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            case EventBrokerType.Kafka:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.ILogger>();
                    return new KafkaEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            case EventBrokerType.GooglePubSub:
                services.AddSingleton<IEventBroker>(provider =>
                {
                    var logger = provider.GetRequiredService<bks.sdk.Observability.Logging.ILogger>();
                    return new GooglePubSubEventBroker(settings.EventBroker.ConnectionString, logger);
                });
                break;

            default:
                // Broker em mem�ria para desenvolvimento
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
        // OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("bks.sdk")
                    .SetSampler(new TraceIdRatioBasedSampler(1.0));

                // Se Jaeger configurado
                if (!string.IsNullOrWhiteSpace(settings.Observability.JaegerEndpoint))
                {
                    builder.AddJaegerExporter(options =>
                    {
                        options.Endpoint = new Uri(settings.Observability.JaegerEndpoint);
                    });
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        // Tracer personalizado
        services.AddSingleton<ITracer, OpenTelemetryTracer>();
    }

    private static void ConfigureTransactions(IServiceCollection services)
    {
        services.AddScoped<ITransactionTokenService, TransactionTokenService>();
    }

    #endregion
}




