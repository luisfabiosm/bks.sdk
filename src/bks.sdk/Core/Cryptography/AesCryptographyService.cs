using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Cryptography
{
    internal sealed class AesCryptographyService : ICryptographyService, IDisposable
    {
        private readonly RandomNumberGenerator _rng;
        private bool _disposed;

        public AesCryptographyService()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public EncryptionResult Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> additionalData = default)
        {
            using var aes = new AesGcm(GenerateRandomBytes(32)); // 256-bit key

            var nonce = GenerateRandomBytes(12); // 96-bit nonce para GCM
            var encryptedData = new byte[data.Length];
            var tag = new byte[16]; // 128-bit tag

            aes.Encrypt(nonce, data, encryptedData, tag, additionalData);

            return new EncryptionResult
            {
                EncryptedData = encryptedData,
                Key = aes.Key.ToArray(),
                Nonce = nonce,
                Tag = tag
            };
        }

        public byte[]? Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> additionalData = default)
        {
            try
            {
                using var aes = new AesGcm(key);
                var decryptedData = new byte[encryptedData.Length];

                aes.Decrypt(nonce, encryptedData, tag, decryptedData, additionalData);

                return decryptedData;
            }
            catch
            {
                return null;
            }
        }

        public string ComputeHash(ReadOnlySpan<byte> data, ReadOnlySpan<byte> salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(data.ToArray(), salt.ToArray(), 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }

        public byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            _rng.GetBytes(bytes);
            return bytes;
        }

        public byte[] DeriveKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, int keyLength)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password.ToArray(), salt.ToArray(), iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keyLength);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _rng?.Dispose();
                _disposed = true;
            }
        }
    }

}
