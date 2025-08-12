using System;
using System.Security.Cryptography;
using System.Text;

namespace bks.sdk.Core.Cryptography
{
    public static class SecureStringGenerator
    {
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string DigitChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        private const string UrlSafeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";


        public static string Generate(int length, StringGenerationOptions options = StringGenerationOptions.All)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero", nameof(length));

            var characterSet = BuildCharacterSet(options);
            if (string.IsNullOrEmpty(characterSet))
                throw new ArgumentException("Invalid character set options", nameof(options));

            using var rng = RandomNumberGenerator.Create();
            var result = new StringBuilder(length);
            var bytes = new byte[4];

            for (var i = 0; i < length; i++)
            {
                rng.GetBytes(bytes);
                var randomValue = BitConverter.ToUInt32(bytes, 0);
                var index = randomValue % characterSet.Length;
                result.Append(characterSet[(int)index]);
            }

            return result.ToString();
        }

 
        public static string GenerateApiKey(int length = 32)
        {
            return Generate(length, StringGenerationOptions.UrlSafe);
        }

        public static string GeneratePassword(int length = 16)
        {
            var password = Generate(length, StringGenerationOptions.All);

            // Garantir que a senha contenha pelo menos um de cada tipo
            var options = StringGenerationOptions.All;
            if (!ContainsCharacterType(password, UppercaseChars))
                password = ReplaceRandomChar(password, UppercaseChars);
            if (!ContainsCharacterType(password, LowercaseChars))
                password = ReplaceRandomChar(password, LowercaseChars);
            if (!ContainsCharacterType(password, DigitChars))
                password = ReplaceRandomChar(password, DigitChars);
            if (!ContainsCharacterType(password, SpecialChars))
                password = ReplaceRandomChar(password, SpecialChars);

            return password;
        }


        public static string GenerateSalt(int length = 16)
        {
            return Generate(length, StringGenerationOptions.Alphanumeric);
        }

  
        public static byte[] GenerateNonce(int length = 12)
        {
            using var rng = RandomNumberGenerator.Create();
            var nonce = new byte[length];
            rng.GetBytes(nonce);
            return nonce;
        }

        private static string BuildCharacterSet(StringGenerationOptions options)
        {
            var characterSet = new StringBuilder();

            if (options.HasFlag(StringGenerationOptions.Uppercase))
                characterSet.Append(UppercaseChars);
            if (options.HasFlag(StringGenerationOptions.Lowercase))
                characterSet.Append(LowercaseChars);
            if (options.HasFlag(StringGenerationOptions.Digits))
                characterSet.Append(DigitChars);
            if (options.HasFlag(StringGenerationOptions.Special))
                characterSet.Append(SpecialChars);
            if (options.HasFlag(StringGenerationOptions.UrlSafe))
                return UrlSafeChars;

            return characterSet.ToString();
        }

        private static bool ContainsCharacterType(string password, string characterSet)
        {
            foreach (var c in password)
            {
                if (characterSet.Contains(c))
                    return true;
            }
            return false;
        }

        private static string ReplaceRandomChar(string password, string characterSet)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);

            var passwordArray = password.ToCharArray();
            var replaceIndex = BitConverter.ToUInt32(bytes, 0) % passwordArray.Length;
            var charIndex = BitConverter.ToUInt32(bytes, 4) % characterSet.Length;

            passwordArray[replaceIndex] = characterSet[(int)charIndex];

            return new string(passwordArray);
        }
    }

}
