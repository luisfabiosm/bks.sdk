namespace Adapters.Inbound.API.DTOs.Response
{
    public record TransacaoResponseDto
    {
        public bool Sucesso { get; init; }
        public string Mensagem { get; init; } = string.Empty;
        public string? TransacaoId { get; init; }
        public decimal? Valor { get; init; }
        public decimal? NovoSaldo { get; init; }
        public string? ProcessadoPor { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

}
