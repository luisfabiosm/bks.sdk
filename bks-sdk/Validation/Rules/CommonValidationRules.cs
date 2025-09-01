using bks.sdk.Validation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Rules;



public class RequiredRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, object?> _propertySelector;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public RequiredRule(string propertyName, Func<T, object?> propertySelector, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        RuleName = $"Required_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} is required";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);

        return value switch
        {
            null => false,
            string str => !string.IsNullOrWhiteSpace(str),
            _ => true
        };
    }
}


public class RangeRule<T, TProperty> : ISyncValidationRule<T>
    where TProperty : IComparable<TProperty>
{
    private readonly Func<T, TProperty> _propertySelector;
    private readonly TProperty _min;
    private readonly TProperty _max;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public RangeRule(string propertyName, Func<T, TProperty> propertySelector, TProperty min, TProperty max, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        _min = min;
        _max = max;
        RuleName = $"Range_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must be between {min} and {max}";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);
        return value.CompareTo(_min) >= 0 && value.CompareTo(_max) <= 0;
    }
}


public class EmailRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public EmailRule(string propertyName, Func<T, string?> propertySelector, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        RuleName = $"Email_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must be a valid email address";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);
        return string.IsNullOrEmpty(value) || EmailRegex.IsMatch(value);
    }
}


public class RegexRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;
    private readonly Regex _regex;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public RegexRule(string propertyName, Func<T, string?> propertySelector, string pattern, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        _regex = new Regex(pattern, RegexOptions.Compiled);
        RuleName = $"Regex_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} format is invalid";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);
        return string.IsNullOrEmpty(value) || _regex.IsMatch(value);
    }
}


public class MinLengthRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;
    private readonly int _minLength;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public MinLengthRule(string propertyName, Func<T, string?> propertySelector, int minLength, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        _minLength = minLength;
        RuleName = $"MinLength_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must have at least {minLength} characters";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);
        return string.IsNullOrEmpty(value) || value.Length >= _minLength;
    }
}


public class MaxLengthRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, string?> _propertySelector;
    private readonly int _maxLength;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public MaxLengthRule(string propertyName, Func<T, string?> propertySelector, int maxLength, string? customMessage = null)
    {
        _propertySelector = propertySelector;
        _maxLength = maxLength;
        RuleName = $"MaxLength_{propertyName}";
        ErrorMessage = customMessage ?? $"{propertyName} must not exceed {maxLength} characters";
    }

    public bool IsValid(T instance)
    {
        var value = _propertySelector(instance);
        return string.IsNullOrEmpty(value) || value.Length <= _maxLength;
    }
}
