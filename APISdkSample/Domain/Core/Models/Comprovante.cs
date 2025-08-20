namespace Domain.Core.Models
{
    public record Comprovante
    {
        public string Numero { get; init; } = string.Empty;
        public string TransacaoId { get; init; } = string.Empty;
        public string Tipo { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public decimal Taxa { get; init; }
        public string ContaOrigem { get; init; } = string.Empty;
        public string ContaDestino { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public DateTime DataHora { get; init; }
    }
}
