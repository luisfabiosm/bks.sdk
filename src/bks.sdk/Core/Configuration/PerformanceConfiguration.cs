using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record PerformanceConfiguration
    {

        public int ObjectPoolSize { get; init; } = 100;


        public int MaxBufferSize { get; init; } = 64 * 1024; // 64KB


        public bool EnableCompression { get; init; } = true;


        public TimeSpan IoTimeout { get; init; } = TimeSpan.FromSeconds(30);


        public int MaxConcurrentConnections { get; init; } = 100;

 
        public CircuitBreakerConfiguration CircuitBreaker { get; init; } = new();


        public RetryConfiguration Retry { get; init; } = new();
    }


}
