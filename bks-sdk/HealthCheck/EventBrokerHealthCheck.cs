using bks.sdk.Core.Configuration;
using bks.sdk.Events;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.HealthCheck
{
    public class EventBrokerHealthCheck : IHealthCheck
    {
        private readonly IEventBroker _eventBroker;
        private readonly SDKSettings _settings;

        public EventBrokerHealthCheck(IEventBroker eventBroker, SDKSettings settings)
        {
            _eventBroker = eventBroker;
            _settings = settings;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["broker_type"] = _settings.EventBroker.BrokerType.ToString(),
                    ["connection_configured"] = !string.IsNullOrWhiteSpace(_settings.EventBroker.ConnectionString)
                };

                // Para implementações reais, você poderia testar uma conexão
                // Para o InMemoryEventBroker, apenas verificar se está instanciado
                if (_eventBroker == null)
                {
                    return HealthCheckResult.Unhealthy(
                        "Event broker not initialized", data: data);
                }

                // Teste básico de funcionalidade
                var testReceived = false;
                await _eventBroker.SubscribeAsync<TestHealthEvent>(evt =>
                {
                    testReceived = true;
                    return Task.CompletedTask;
                });

                await _eventBroker.PublishAsync(new TestHealthEvent());

                // Aguardar um pouco para processamento assíncrono
                await Task.Delay(100, cancellationToken);

                data["test_publish_subscribe"] = testReceived;

                return testReceived
                    ? HealthCheckResult.Healthy("Event broker is working properly", data: data)
                    : HealthCheckResult.Degraded("Event broker test failed", data: data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Event broker health check failed", ex);
            }
        }

        private class TestHealthEvent : Events.DomainEvent
        {
            public override string EventType => "health.test";
        }
    }
}
