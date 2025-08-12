using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{

    public record StorageConfiguration
    {
    
        public StorageProvider Provider { get; init; } = StorageProvider.InMemory;


        public string? SqlServerConnectionString { get; init; }


        public string? PostgreSqlConnectionString { get; init; }


        public string? MongoConnectionString { get; init; }


        public string DatabaseName { get; init; } = "BksTokens";

 
        public string TokenTableName { get; init; } = "SecureTokens";

        public TimeSpan DefaultTokenExpiration { get; init; } = TimeSpan.FromDays(30);


        public bool EnableAutomaticCleanup { get; init; } = true;


        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromHours(6);
    }

}
