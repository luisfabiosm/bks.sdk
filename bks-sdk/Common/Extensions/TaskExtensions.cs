using bks.sdk.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Extensions;

public static class TaskExtensions
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Task did not complete within {timeout}");
        }

        return await task;
    }

    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Task did not complete within {timeout}");
        }

        await task;
    }

    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return await task;
    }

    public static async Task<Result<T>> ToResult<T>(this Task<T> task)
    {
        try
        {
            var result = await task;
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    public static async Task<Result> ToResult(this Task task)
    {
        try
        {
            await task;
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public static Task<T[]> WhenAllSafe<T>(this IEnumerable<Task<T>> tasks)
    {
        return Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                return await task;
            }
            catch
            {
                return default(T)!;
            }
        }));
    }

    public static async Task<IEnumerable<Result<T>>> WhenAllWithResults<T>(this IEnumerable<Task<T>> tasks)
    {
        var results = await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                var result = await task;
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex.Message);
            }
        }));

        return results;
    }
}
