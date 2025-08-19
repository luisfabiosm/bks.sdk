using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Transactions
{
    public class TransactionTokenData
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string IntegrityHash { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
