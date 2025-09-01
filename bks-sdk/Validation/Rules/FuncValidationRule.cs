using bks.sdk.Validation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Validation.Rules;

public class FuncValidationRule<T> : ISyncValidationRule<T>
{
    private readonly Func<T, bool> _predicate;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public FuncValidationRule(string ruleName, string errorMessage, Func<T, bool> predicate)
    {
        RuleName = ruleName;
        ErrorMessage = errorMessage;
        _predicate = predicate;
    }

    public bool IsValid(T instance)
    {
        return _predicate(instance);
    }
}

public class AsyncFuncValidationRule<T> : IAsyncValidationRule<T>
{
    private readonly Func<T, Task<bool>> _asyncPredicate;

    public string RuleName { get; }
    public string ErrorMessage { get; }

    public AsyncFuncValidationRule(string ruleName, string errorMessage, Func<T, Task<bool>> asyncPredicate)
    {
        RuleName = ruleName;
        ErrorMessage = errorMessage;
        _asyncPredicate = asyncPredicate;
    }

    public async Task<bool> IsValidAsync(T instance, CancellationToken cancellationToken = default)
    {
        return await _asyncPredicate(instance);
    }
}