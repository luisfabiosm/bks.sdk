namespace Domain.Core.Models
{
    public record FraudeConfiguration
    {
        public int LimiteScoreRisco { get; init; } = 50; // Score acima do qual é considerado risco
        public int LimiteTransacoesPorHora { get; init; } = 10;
        public int LimiteTransacoesPorDia { get; init; } = 50;
        public decimal ValorLimiteAltoRisco { get; init; } = 50000m;
        public TimeSpan JanelaAnalise { get; init; } = TimeSpan.FromHours(24);
    }
}
