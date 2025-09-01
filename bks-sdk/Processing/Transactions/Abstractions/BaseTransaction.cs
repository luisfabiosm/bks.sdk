using System.Text.Json;

public abstract record BaseTransaction
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string TransactionType => GetType().Name;
    public Dictionary<string, object> Metadata { get; init; } = new();

    public virtual string Serialize()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(this, GetType(), options);
    }

    public static T? Deserialize<T>(string json) where T : BaseTransaction
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch
        {
            return null;
        }
    }
}
