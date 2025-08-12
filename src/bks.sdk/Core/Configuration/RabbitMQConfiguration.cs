using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration;
public record RabbitMQConfiguration
{
    public required string HostName { get; init; }
    public int Port { get; init; } = 5672;
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public string VirtualHost { get; init; } = "/";
    public bool UseSsl { get; init; } = false;
}