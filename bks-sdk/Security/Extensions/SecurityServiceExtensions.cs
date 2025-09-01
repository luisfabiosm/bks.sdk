using bks.sdk.Core.Configuration;
using bks.sdk.Security.Authentication;
using bks.sdk.Security.Authorization;
using bks.sdk.Security.Encryption;
using bks.sdk.Security.Licensing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using System.Text;


namespace bks.sdk.Security.Extensions;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddBKSFrameworkSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new BKSFrameworkSettings();
        configuration.GetSection("BKSFramework").Bind(settings);

        // Validação de licença
        services.AddSingleton<ILicenseValidator, LicenseValidator>();

        // Criptografia de dados
        if (settings.Security.DataEncryption.Enabled)
        {
            services.AddSingleton<IDataEncryptor, AesDataEncryptor>();
        }
        else
        {
            services.AddSingleton<IDataEncryptor, NoOpDataEncryptor>();
        }

        // JWT Token Provider
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();

        // Configurar autenticação JWT
        if (!string.IsNullOrWhiteSpace(settings.Security.Jwt.SecretKey))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // Para desenvolvimento
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(settings.Security.Jwt.SecretKey)),
                        ValidateIssuer = true,
                        ValidIssuer = settings.Security.Jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Security.Jwt.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        // Autorização customizada
        services.AddAuthorization();
        services.AddScoped<IAuthorizationHandler, BKSAuthorizationHandler>();
        services.AddHttpContextAccessor();

        // Validar licença na inicialização
        ValidateLicenseOnStartup(services, settings);

        return services;
    }

    private static void ValidateLicenseOnStartup(IServiceCollection services, BKSFrameworkSettings settings)
    {
        // Criar um scope temporário para validar a licença
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var licenseValidator = scope.ServiceProvider.GetRequiredService<ILicenseValidator>();

        var result = licenseValidator.ValidateLicenseDetailed(settings.LicenseKey, settings.ApplicationName);

        if (!result.IsValid)
        {
            throw new UnauthorizedAccessException($"Licença inválida: {result.Error}");
        }
    }
}

