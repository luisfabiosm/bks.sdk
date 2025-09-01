using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Results;

public class ValidationResult
{
    public bool IsValid { get; }
    public List<string> Errors { get; }
    public bool IsInvalid => !IsValid;

    private ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? new List<string>();
    }

    public static ValidationResult Success() => new(true, new List<string>());
    public static ValidationResult Failure(List<string> errors) => new(false, errors ?? new List<string>());
    public static ValidationResult Failure(string error) => new(false, new List<string> { error });
    public static ValidationResult Failure(params string[] errors) => new(false, errors?.ToList() ?? new List<string>());

    public ValidationResult AddError(string error)
    {
        if (IsValid)
            throw new InvalidOperationException("Cannot add error to a valid result");

        Errors.Add(error);
        return this;
    }

    public ValidationResult AddErrors(IEnumerable<string> errors)
    {
        if (IsValid)
            throw new InvalidOperationException("Cannot add errors to a valid result");

        Errors.AddRange(errors);
        return this;
    }

    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var allErrors = results.SelectMany(r => r.Errors).ToList();
        return allErrors.Count == 0 ? Success() : Failure(allErrors);
    }

    public static ValidationResult operator +(ValidationResult left, ValidationResult right)
    {
        return Combine(left, right);
    }

    public override string ToString()
    {
        return IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
    }
}
