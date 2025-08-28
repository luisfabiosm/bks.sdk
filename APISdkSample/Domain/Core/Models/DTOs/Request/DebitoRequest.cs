namespace Domain.Core.Models.DTOs.Request
{
    public record DebitoRequest
    {
        public int Agencia { get; init; } = 1;
        public int NumeroConta { get; init; } 
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
    }
}
