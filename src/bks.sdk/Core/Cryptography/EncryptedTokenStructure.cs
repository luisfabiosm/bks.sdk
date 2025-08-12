using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    internal record EncryptedTokenStructure
    {
        public required int Version { get; init; }
        public required string TokenId { get; init; }
        public required string EncryptedData { get; init; }
        public required string Nonce { get; init; }
        public required string Tag { get; init; }
        public required string Algorithm { get; init; }
    }
}
