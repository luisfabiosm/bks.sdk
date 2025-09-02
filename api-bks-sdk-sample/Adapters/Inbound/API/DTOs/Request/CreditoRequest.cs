namespace Adapters.Inbound.API.DTOs.Request
{
    public record CreditoRequestDto
    {
        public int NumeroConta { get; init; } 
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
    }
}
