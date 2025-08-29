using bks.sdk.Observability.Correlation;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Observability.Enrichers;

public class CorrelationEnricher : ILogEventEnricher
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationEnricher(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!string.IsNullOrWhiteSpace(_correlationContextAccessor.CorrelationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "CorrelationId", _correlationContextAccessor.CorrelationId));
        }

        if (!string.IsNullOrWhiteSpace(_correlationContextAccessor.UserId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "UserId", _correlationContextAccessor.UserId));
        }

        if (!string.IsNullOrWhiteSpace(_correlationContextAccessor.UserName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "UserName", _correlationContextAccessor.UserName));
        }

        if (!string.IsNullOrWhiteSpace(_correlationContextAccessor.IpAddress))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ClientIpAddress", _correlationContextAccessor.IpAddress));
        }

        // Adicionar propriedades customizadas
        foreach (var property in _correlationContextAccessor.Properties)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                $"Context_{property.Key}", property.Value, destructureObjects: true));
        }
    }
}

