namespace Adapters.Inbound.API.DTOs.Request
{
    public record CriarContaRequestDto
    {
        public int Numero { get; init; } 
        public string Titular { get; init; } = string.Empty;
        public decimal SaldoInicial { get; init; } = 0;
    }
}
