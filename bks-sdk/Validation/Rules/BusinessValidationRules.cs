using bks.sdk.Validation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Rules;
public class PositiveNumberRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, decimal> _propertySelector;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public PositiveNumberRule(string propertyName, Func<T, decimal> propertySelector, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        RuleName = $"PositiveNumber_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must be greater than zero";
    }

    public bool IsValid(T instance)
    {
        return _propertySelector(instance) > 0;
    }
}

public class CPFRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public CPFRule(string propertyName, Func<T, string?> propertySelector, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        RuleName = $"CPF_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must be a valid CPF";
    }

    public bool IsValid(T instance)
    {
        var cpf = _propertySelector(instance);
        return string.IsNullOrEmpty(cpf) || IsValidCPF(cpf);
    }

    private static bool IsValidCPF(string cpf)
    {
        // Remove formatação
        cpf = new string(cpf.Where(char.IsDigit).ToArray());

        if (cpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cpf.All(digit => digit == cpf[0]))
            return false;

        // Calcula os dígitos verificadores
        var sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += int.Parse(cpf[i].ToString()) * (10 - i);
        }

        var firstDigit = (sum * 10) % 11;
        if (firstDigit == 10) firstDigit = 0;

        if (int.Parse(cpf[9].ToString()) != firstDigit)
            return false;

        sum = 0;
        for (int i = 0; i < 10; i++)
        {
            sum += int.Parse(cpf[i].ToString()) * (11 - i);
        }

        var secondDigit = (sum * 10) % 11;
        if (secondDigit == 10) secondDigit = 0;

        return int.Parse(cpf[10].ToString()) == secondDigit;
    }
}

public class CNPJRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public CNPJRule(string propertyName, Func<T, string?> propertySelector, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        RuleName = $"CNPJ_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must be a valid CNPJ";
    }

    public bool IsValid(T instance)
    {
        var cnpj = _propertySelector(instance);
        return string.IsNullOrEmpty(cnpj) || IsValidCNPJ(cnpj);
    }

    private static bool IsValidCNPJ(string cnpj)
    {
        // Remove formatação
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

        if (cnpj.Length != 14)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cnpj.All(digit => digit == cnpj[0]))
            return false;

        // Calcula o primeiro dígito verificador
        var sum = 0;
        var multipliers1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        for (int i = 0; i < 12; i++)
        {
            sum += int.Parse(cnpj[i].ToString()) * multipliers1[i];
        }

        var firstDigit = sum % 11;
        firstDigit = firstDigit < 2 ? 0 : 11 - firstDigit;

        if (int.Parse(cnpj[12].ToString()) != firstDigit)
            return false;

        // Calcula o segundo dígito verificador
        sum = 0;
        var multipliers2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        for (int i = 0; i < 13; i++)
        {
            sum += int.Parse(cnpj[i].ToString()) * multipliers2[i];
        }

        var secondDigit = sum % 11;
        secondDigit = secondDigit < 2 ? 0 : 11 - secondDigit;

        return int.Parse(cnpj[13].ToString()) == secondDigit;
    }
}
