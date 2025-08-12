using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record MessagingConfiguration
    {

        public MessagingProvider Provider { get; init; } = MessagingProvider.None;

 
        public string? AzureServiceBusConnectionString { get; init; }


        public RabbitMQConfiguration? RabbitMQ { get; init; }

   
        public KafkaConfiguration? Kafka { get; init; }

        public bool EnableRetry { get; init; } = true;


        public int MaxRetryAttempts { get; init; } = 3;

  
        public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    
        public TimeSpan PublishTimeout { get; init; } = TimeSpan.FromSeconds(10);
    }

}
