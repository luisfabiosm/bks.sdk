using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Configuration
{
    public record RetryConfiguration
    {
 
        public int MaxAttempts { get; init; } = 3;


        public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);


        public double BackoffMultiplier { get; init; } = 2.0;

 
        public TimeSpan MaxJitter { get; init; } = TimeSpan.FromMilliseconds(100);
    }

}
