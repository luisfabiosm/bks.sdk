using bks.sdk.Core.Configuration;
using bks.sdk.Core.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Storage
{
    internal static class StorageFactory
    {
        /// <summary>
        /// Cria uma instância de ITokenStorage baseada na configuração
        /// </summary>
        public static ITokenStorage CreateTokenStorage(IOptions<BksConfiguration> configuration, IBksLogger logger)
        {
            var config = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));

            return config.Storage.Provider switch
            {
                StorageProvider.InMemory => new InMemoryTokenStorage(logger),
                StorageProvider.SqlServer => new SqlServerTokenStorage(configuration, logger),
                StorageProvider.PostgreSql => new PostgreSqlTokenStorage(configuration, logger),
                StorageProvider.MongoDB => new MongoTokenStorage(configuration, logger),
                StorageProvider.Redis => new RedisTokenStorage(configuration, logger),
                _ => throw new NotSupportedException($"Storage provider {config.Storage.Provider} is not supported")
            };
        }
    }
}
