using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    public interface ICryptographyService
    {

        EncryptionResult Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> additionalData = default);


        byte[]? Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> additionalData = default);


        string ComputeHash(ReadOnlySpan<byte> data, ReadOnlySpan<byte> salt);


        byte[] GenerateRandomBytes(int length);

        byte[] DeriveKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, int keyLength);
    }

}
