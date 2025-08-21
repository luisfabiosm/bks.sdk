using Domain.Core.Enums;

namespace Domain.Core.Models
{
    public record AnaliseRisco
    {
        public bool IsRisco { get; init; }
        public TipoNivelRisco NivelRisco { get; init; }
        public int ScoreRisco { get; init; } // 0-100, onde 100 é muito alto risco
        public List<string> Motivos { get; init; } = new();
        public List<RegrafValidada> RegrasValidadas { get; init; } = new();
        public DateTime DataAnalise { get; init; } = DateTime.UtcNow;
        public string? RecomendacaoAcao { get; init; }
    }
}
