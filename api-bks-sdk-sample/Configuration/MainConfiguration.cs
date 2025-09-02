using Adapters.Inbound.API.Endpoints;
using Adapters.Outbound.DataAdapter;
using bks.sdk.Common.Enums;
using bks.sdk.Core.Initialization;
using bks.sdk.Events.Abstractions;
using bks.sdk.Events.Pipeline;
using bks.sdk.Processing.Abstractions;
using bks.sdk.Processing.Mediator.Abstractions;
using Domain.Core.Commands;
using Domain.Core.Ports.Domain;
using Domain.Core.Ports.Outbound;
using Domain.Core.Transactions;
using Domain.EventHandlers;
using Domain.Processors;
using Domain.Services;
using Domain.UseCases;
using Domain.Validators;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace Configuration
{
    public static class MainConfiguration
    {

        public static IServiceCollection ConfigureApiInjections(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddBKSFramework(configuration, options =>
            {
                // Configurar para usar ambos os modos (Mediator para crédito, Transaction Processor para débito)
                options.ProcessingMode = ProcessingMode.Mediator; // Padrão será Mediator
                options.EnableValidation = true;
                options.EnableEvents = true;
            });

            // Registrar repositórios
            services.AddSingleton<IContaRepository, InMemoryContaRepository>();

            // Registrar handlers do Mediator (para crédito)
            services.AddScoped<IBKSRequestHandler<ProcessarCreditoCommand, ProcessarCreditoResponse>, UseCaseProcessarCreditoHandler>();

            // Registrar processador de transação (para débito)
            services.AddScoped<IBKSTransactionProcessor<DebitoTransaction, DebitoResponse>, DebitoProcessor>();

            // Registrar validadores
            services.AddScoped<ProcessarCreditoCommandValidator>();
            services.AddScoped<DebitoTransactionValidator>();

            // Registrar serviços de apoio
            services.AddSingleton<IAuditService, AuditService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IAlertService, AlertService>();
            services.AddSingleton<MonitoringService>();

            // Registrar event handlers
            services.AddScoped<TransactionStartedEventHandler>();
            services.AddScoped<TransactionCompletedEventHandler>();
            services.AddScoped<TransactionFailedEventHandler>();

            // Configuração da API
            services.AddEndpointsApiExplorer();
         
            return services;
        }

        public static void UseApiExtensions(this WebApplication app)
        {
            ConfigureEventHandlers(app.Services);


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BKS SDK Example v1");
                    c.RoutePrefix = string.Empty; // Swagger na raiz
                    c.DisplayRequestDuration();
                    c.EnableFilter();
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                });
            }

            app.UseBKSFramework();

            app.AddTransactionEndpoints();

            //PerformStartupHealthCheck(app.Services);

            app.Run();
        }

        static async Task ConfigureEventHandlers(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var eventSubscriber = scope.ServiceProvider.GetService<IEventSubscriber>();

            if (eventSubscriber != null)
            {
                var startedHandler = scope.ServiceProvider.GetRequiredService<TransactionStartedEventHandler>();
                var completedHandler = scope.ServiceProvider.GetRequiredService<TransactionCompletedEventHandler>();
                var failedHandler = scope.ServiceProvider.GetRequiredService<TransactionFailedEventHandler>();

                await eventSubscriber.SubscribeAsync(startedHandler);
                await eventSubscriber.SubscribeAsync(completedHandler);
                await eventSubscriber.SubscribeAsync(failedHandler);

                Console.WriteLine("✅ Event handlers configurados com sucesso");
            }
        }


        static async Task PerformStartupHealthCheck(IServiceProvider services)
        {
            try
            {
                using var scope = services.CreateScope();
                var monitoringService = scope.ServiceProvider.GetService<MonitoringService>();

                if (monitoringService != null)
                {
                    await monitoringService.PerformHealthCheckAsync();
                    Console.WriteLine("✅ Health check inicial concluído");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Health check inicial falhou: {ex.Message}");
            }
        }

    }
}
