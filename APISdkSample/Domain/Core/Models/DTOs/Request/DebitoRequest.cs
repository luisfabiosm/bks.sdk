namespace Domain.Core.Models.DTOs.Request
{
    public record DebitoRequest
    {
        public string NumeroConta { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public string? TipoDebito { get; init; } = "OPERACIONAL";
    }
}
