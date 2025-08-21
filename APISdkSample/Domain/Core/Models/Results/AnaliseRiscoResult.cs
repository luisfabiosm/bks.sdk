namespace Domain.Core.Models.Results
{
    public record AnaliseRiscoResult
    {
        public bool IsRisco { get; init; }
        public string NivelRisco { get; init; } = string.Empty; // BAIXO, MÉDIO, ALTO
        public List<string> Motivos { get; init; } = new();
    }
}
