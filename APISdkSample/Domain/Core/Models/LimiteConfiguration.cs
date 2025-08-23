namespace Domain.Core.Models
{
    public record LimiteConfiguration
    {
        public decimal LimiteMaximoPorTransacao { get; init; } = 100000m;
        public TimeSpan CacheExpiration { get; init; } = TimeSpan.FromHours(24);
    }
}
