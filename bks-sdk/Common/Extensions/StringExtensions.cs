using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bks.sdk.Common.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool HasValue(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static string ToTitleCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
    }

    public static string ToPascalCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return Regex.Replace(value, @"(?:^|_)([a-z])", match => match.Groups[1].Value.ToUpper());
    }

    public static string ToCamelCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        var pascalCase = value.ToPascalCase();
        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }

    public static string ToKebabCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return Regex.Replace(value, @"(?<!^)([A-Z])", "-$1").ToLower();
    }

    public static string ToSnakeCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return Regex.Replace(value, @"(?<!^)([A-Z])", "_$1").ToLower();
    }

    public static string RemoveSpecialCharacters(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return Regex.Replace(value, @"[^a-zA-Z0-9\s]", "");
    }

    public static string OnlyNumbers(this string value)
    {
        if (value.IsNullOrEmpty())
            return value;

        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value.IsNullOrEmpty() || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    public static string Mask(this string value, char maskChar = '*', int visibleStart = 0, int visibleEnd = 0)
    {
        if (value.IsNullOrEmpty())
            return value;

        if (visibleStart + visibleEnd >= value.Length)
            return value;

        var start = value.Substring(0, visibleStart);
        var middle = new string(maskChar, value.Length - visibleStart - visibleEnd);
        var end = visibleEnd > 0 ? value.Substring(value.Length - visibleEnd) : "";

        return start + middle + end;
    }

    public static string Hash(this string value, HashAlgorithmName algorithm = default)
    {
        if (value.IsNullOrEmpty())
            return value;

        var hashAlgorithm = algorithm == default ? HashAlgorithmName.SHA256 : algorithm;

        using var hash = HashAlgorithm.Create(hashAlgorithm.Name!)!;
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = hash.ComputeHash(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool IsValidEmail(this string email)
    {
        if (email.IsNullOrWhiteSpace())
            return false;

        const string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    public static bool IsValidCPF(this string cpf)
    {
        if (cpf.IsNullOrWhiteSpace())
            return false;

        cpf = cpf.OnlyNumbers();

        if (cpf.Length != 11 || cpf.All(c => c == cpf[0]))
            return false;

        // Calculate first check digit
        var sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        var firstDigit = (sum * 10) % 11;
        if (firstDigit == 10) firstDigit = 0;

        if (int.Parse(cpf[9].ToString()) != firstDigit)
            return false;

        // Calculate second check digit
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        var secondDigit = (sum * 10) % 11;
        if (secondDigit == 10) secondDigit = 0;

        return int.Parse(cpf[10].ToString()) == secondDigit;
    }

    public static bool IsValidCNPJ(this string cnpj)
    {
        if (cnpj.IsNullOrWhiteSpace())
            return false;

        cnpj = cnpj.OnlyNumbers();

        if (cnpj.Length != 14 || cnpj.All(c => c == cnpj[0]))
            return false;

        // Calculate first check digit
        var multipliers1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = cnpj.Take(12).Select((c, i) => int.Parse(c.ToString()) * multipliers1[i]).Sum();
        var firstDigit = sum % 11;
        firstDigit = firstDigit < 2 ? 0 : 11 - firstDigit;

        if (int.Parse(cnpj[12].ToString()) != firstDigit)
            return false;

        // Calculate second check digit
        var multipliers2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = cnpj.Take(13).Select((c, i) => int.Parse(c.ToString()) * multipliers2[i]).Sum();
        var secondDigit = sum % 11;
        secondDigit = secondDigit < 2 ? 0 : 11 - secondDigit;

        return int.Parse(cnpj[13].ToString()) == secondDigit;
    }
}
