using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Helpers;

public static class RetryHelper
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var actualDelay = delay ?? TimeSpan.FromSeconds(1);
        var actualShouldRetry = shouldRetry ?? (_ => true);

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts || !actualShouldRetry(ex))
                {
                    throw;
                }

                await Task.Delay(actualDelay * attempt); // Exponential backoff
            }
        }

        throw lastException!;
    }

    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, maxAttempts, delay, shouldRetry);
    }

    public static T ExecuteWithRetry<T>(
        Func<T> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var actualDelay = delay ?? TimeSpan.FromSeconds(1);
        var actualShouldRetry = shouldRetry ?? (_ => true);

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts || !actualShouldRetry(ex))
                {
                    throw;
                }

                Thread.Sleep(actualDelay * attempt); // Exponential backoff
            }
        }

        throw lastException!;
    }
}


