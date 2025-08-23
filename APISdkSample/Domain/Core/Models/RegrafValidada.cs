namespace Domain.Core.Models
{
    public record RegrafValidada
    {
        public string NomeRegra { get; init; } = string.Empty;
        public bool Violada { get; init; }
        public int PesoRisco { get; init; } // 1-10
        public string? Detalhes { get; init; }
    }
}
