using bks.sdk.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace bks.sdk.Common.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T obj, bool prettyPrint = false)
    {
        return JsonSerializer.Serialize(obj, prettyPrint ? PrettyOptions : DefaultOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    public static object? Deserialize(string json, Type type)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize(json, type, DefaultOptions);
        }
        catch
        {
            return null;
        }
    }

    public static Result<T> TryDeserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<T>.Failure("JSON string is null or empty");

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return result != null
                ? Result<T>.Success(result)
                : Result<T>.Failure("Deserialization returned null");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"JSON deserialization failed: {ex.Message}");
        }
    }

    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Dictionary<string, object?> ToDictionary(object obj)
    {
        var json = Serialize(obj);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, DefaultOptions)
               ?? new Dictionary<string, object?>();
    }

    public static T FromDictionary<T>(Dictionary<string, object?> dictionary)
    {
        var json = Serialize(dictionary);
        return Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize dictionary");
    }
}

