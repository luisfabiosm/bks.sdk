namespace Domain.Core.Models
{
    public record PerfilRisco
    {
        public int NumeroConta { get; init; } 
        public int ScoreComportamento { get; init; } // 0-100
        public int QuantidadeTransacoesUltimos30Dias { get; init; }
        public decimal VolumeTransacoesUltimos30Dias { get; init; }
        public List<string> PadroesIdentificados { get; init; } = new();
        public DateTime UltimaAtualizacao { get; init; }
        public bool ContaBloqueada { get; init; }
        public List<EventoSuspeito> EventosSuspeitos { get; init; } = new();
    }
}
