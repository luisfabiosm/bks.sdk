namespace Domain.Core.Transactions
{
    public record DebitoResponse
    {
        public bool Sucesso { get; init; }
        public string? Mensagem { get; init; }
        public decimal NovoSaldo { get; init; }
        public string ContaId { get; init; } = string.Empty;
        public string MovimentacaoId { get; init; } = string.Empty;
        public DateTime DataProcessamento { get; init; } = DateTime.UtcNow;
        public decimal ValorDebitado { get; init; }
        public decimal SaldoAnterior { get; init; }
    }

}
