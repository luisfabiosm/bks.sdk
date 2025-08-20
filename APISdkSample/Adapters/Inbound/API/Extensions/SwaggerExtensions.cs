using Microsoft.OpenApi.Models;

namespace Adapters.Inbound.API.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "APi Sdk Sample",
                    Version = "v1.0.0",
                    Description = "API para processamento de transações financeiras usando bks-sdk",
                    Contact = new OpenApiContact
                    {
                        Name = "Equipe Backside",
                        Email = "fabio.backside@gmail.com",
                        Url = new Uri("https://bks.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proprietary",
                        Url = new Uri("https://bks.com/license")
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header usando o esquema Bearer. Exemplo: 'Bearer {token}'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }


        public static void UseSwaggerExtensions(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BKS Transações API v1");
                    c.RoutePrefix = string.Empty; // Swagger na raiz
                    c.DisplayRequestDuration();
                    c.EnableFilter();
                    c.ShowExtensions();
                });
            }
        }
    }
}
