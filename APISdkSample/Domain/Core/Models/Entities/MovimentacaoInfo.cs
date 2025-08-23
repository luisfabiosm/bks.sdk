using Domain.Core.Enums;

namespace Domain.Core.Models.Entities
{
    public record MovimentacaoInfo
    {
        public string Id { get; init; } = string.Empty;
        public string ContaId { get; init; } = string.Empty;
        public TipoMovimentacao Tipo { get; init; }
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public DateTime DataMovimentacao { get; init; }
        public decimal SaldoAnterior { get; init; }
        public decimal SaldoPosterior { get; init; }
    }
}
