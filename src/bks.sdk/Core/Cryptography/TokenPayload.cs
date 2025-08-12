using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    internal record TokenPayload
    {
        public required string TokenId { get; init; }
        public required string Data { get; init; }
        public required long CreatedAt { get; init; }
        public long? ExpiresAt { get; init; }
        public required string DataType { get; init; }
    }
}
