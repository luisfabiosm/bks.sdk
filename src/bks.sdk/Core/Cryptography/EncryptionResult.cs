using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    public record EncryptionResult
    {
        public required byte[] EncryptedData { get; init; }
        public required byte[] Key { get; init; }
        public required byte[] Nonce { get; init; }
        public required byte[] Tag { get; init; }
    }
}
