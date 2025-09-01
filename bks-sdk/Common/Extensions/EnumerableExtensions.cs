using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Common.Extensions;

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    public static bool HasItems<T>(this IEnumerable<T>? source)
    {
        return source != null && source.Any();
    }

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    public static IEnumerable<TResult> SelectNotNull<T, TResult>(this IEnumerable<T> source, Func<T, TResult?> selector)
        where TResult : class
    {
        return source.Select(selector).Where(x => x != null)!;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(x => x != null)!;
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        foreach (var item in source)
        {
            await action(item);
        }
    }

    public static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, Task<TResult>> selector)
    {
        var tasks = source.Select(selector);
        return await Task.WhenAll(tasks);
    }

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));

        var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetChunk(enumerator, chunkSize);
        }
    }

    private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, int chunkSize)
    {
        var count = 0;
        do
        {
            yield return enumerator.Current;
            count++;
        }
        while (count < chunkSize && enumerator.MoveNext());
    }

    public static T? FirstOrDefault<T>(this IEnumerable<T> source, T? defaultValue)
    {
        return source.FirstOrDefault() ?? defaultValue;
    }

    public static decimal SafeSum<T>(this IEnumerable<T> source, Func<T, decimal> selector)
    {
        return source?.Sum(selector) ?? 0;
    }

    public static double SafeAverage<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        return source?.Any() == true ? source.Average(selector) : 0;
    }

    public static string JoinString<T>(this IEnumerable<T> source, string separator = ", ")
    {
        return string.Join(separator, source);
    }

    public static Dictionary<TKey, TValue> ToDictionarySafe<T, TKey, TValue>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = valueSelector(item);
            }
        }
        return dictionary;
    }
}
