using System.ComponentModel.DataAnnotations;

namespace bks.sdk.Core.Configuration
{
    public record BksConfiguration
    {

        [Required]
        public required string ApplicationKey { get; init; }

     
        public BksConfigurationSource ConfigurationSource { get; init; } = BksConfigurationSource.AppSettings;

 
        public string? RedisConnectionString { get; init; }

  
        public string RedisKeyPrefix { get; init; } = "bks:";

        public SecurityConfiguration Security { get; init; } = new();

    
        public MessagingConfiguration Messaging { get; init; } = new();

   
        public ObservabilityConfiguration Observability { get; init; } = new();

       
        public StorageConfiguration Storage { get; init; } = new();

     
        public JwtConfiguration? Jwt { get; init; }

     
        public bool EnableJWT { get; init; } = false;

   
        public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

        public PerformanceConfiguration Performance { get; init; } = new();
    }

}
