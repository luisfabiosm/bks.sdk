using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Results;


public static class ResultExtensions
{
    // Extensões para Result
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();

        return result;
    }

    public static Result OnFailure(this Result result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error!);

        return result;
    }

    public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
            await action();

        return result;
    }

    public static async Task<Result> OnFailureAsync(this Result result, Func<string, Task> action)
    {
        if (result.IsFailure)
            await action(result.Error!);

        return result;
    }

    // Extensões para Result<T>
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> mapper)
    {
        return result.IsSuccess
            ? Result<U>.Success(mapper(result.Value!))
            : Result<U>.Failure(result.Error!);
    }

    public static async Task<Result<U>> MapAsync<T, U>(this Result<T> result, Func<T, Task<U>> mapper)
    {
        return result.IsSuccess
            ? Result<U>.Success(await mapper(result.Value!))
            : Result<U>.Failure(result.Error!);
    }

    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value!)
            : Result<U>.Failure(result.Error!);
    }

    public static async Task<Result<U>> BindAsync<T, U>(this Result<T> result, Func<T, Task<Result<U>>> binder)
    {
        return result.IsSuccess
            ? await binder(result.Value!)
            : Result<U>.Failure(result.Error!);
    }

    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value!);

        return result;
    }

    public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
    {
        if (result.IsFailure)
            action(result.Error!);

        return result;
    }

    public static T Match<T, U>(this Result<U> result, Func<U, T> onSuccess, Func<string, T> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.Error!);
    }

    public static async Task<T> MatchAsync<T, U>(this Result<U> result, Func<U, Task<T>> onSuccess, Func<string, Task<T>> onFailure)
    {
        return result.IsSuccess ? await onSuccess(result.Value!) : await onFailure(result.Error!);
    }

    // Combinação de múltiplos resultados
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();
        if (failures.Count == 0)
            return Result.Success();

        var errors = string.Join("; ", failures.Select(f => f.Error));
        return Result.Failure(errors);
    }

    public static Result<IEnumerable<T>> Combine<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();
        if (failures.Count == 0)
            return Result<IEnumerable<T>>.Success(results.Select(r => r.Value!));

        var errors = string.Join("; ", failures.Select(f => f.Error));
        return Result<IEnumerable<T>>.Failure(errors);
    }

    // Conversão para e de ValidationResult
    public static Result ToResult(this ValidationResult validationResult)
    {
        return validationResult.IsValid
            ? Result.Success()
            : Result.Failure(string.Join("; ", validationResult.Errors));
    }

    public static Result<T> ToResult<T>(this ValidationResult validationResult, T value)
    {
        return validationResult.IsValid
            ? Result<T>.Success(value)
            : Result<T>.Failure(string.Join("; ", validationResult.Errors));
    }
}
