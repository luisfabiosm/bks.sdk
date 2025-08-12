using System;
using System.Security.Cryptography;
using System.Text;

namespace bks.sdk.Core.Cryptography
{
    public static class TokenHasher
    {
  
        public static string ComputeSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

   
        public static string ComputeSha256Hash(string input, string salt)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var combined = input + salt;
            return ComputeSha256Hash(combined);
        }

   
        public static string ComputePbkdf2Hash(string input, string salt, int iterations = 100000)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var inputBytes = Encoding.UTF8.GetBytes(input);
            var saltBytes = Encoding.UTF8.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(inputBytes, saltBytes, iterations, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);

            return Convert.ToBase64String(hashBytes);
        }

  
        public static bool VerifyHash(string input, string hash, string salt, HashType hashType = HashType.SHA256)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(hash))
                return false;

            var computedHash = hashType switch
            {
                HashType.SHA256 => ComputeSha256Hash(input, salt),
                HashType.PBKDF2 => ComputePbkdf2Hash(input, salt),
                _ => throw new ArgumentException("Unsupported hash type", nameof(hashType))
            };

            return string.Equals(hash, computedHash, StringComparison.Ordinal);
        }


        public static string GenerateUniqueId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = Random.Shared.Next(10000, 99999);

            return $"{timestamp:x}{random:x}";
        }

  
        public static string GenerateSessionToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);

            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }

}
