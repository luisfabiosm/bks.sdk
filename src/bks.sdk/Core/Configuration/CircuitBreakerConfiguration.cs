using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record CircuitBreakerConfiguration
    {
  
        public int FailureThreshold { get; init; } = 5;

        public TimeSpan OpenTimeout { get; init; } = TimeSpan.FromSeconds(30);

        public TimeSpan HalfOpenTimeout { get; init; } = TimeSpan.FromSeconds(5);
    }

}
