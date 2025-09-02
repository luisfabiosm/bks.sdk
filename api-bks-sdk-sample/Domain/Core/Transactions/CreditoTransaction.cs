using Microsoft.AspNetCore.Http.HttpResults;

namespace Domain.Core.Transactions
{
    public record CreditoTransaction : BaseTransaction
    {
        public string NumeroContaCredito { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public string? Observacoes { get; init; }

        // Regras de negócio específicas da transação de crédito
        public bool IsValorValido => Valor > 0 && Valor <= 1_000_000; // Limite de 1 milhão
        public bool IsContaValida => !string.IsNullOrWhiteSpace(NumeroContaCredito);
        public bool IsDescricaoValida => !string.IsNullOrWhiteSpace(Descricao) && Descricao.Length <= 200;

        public override string Serialize()
        {
            var data = new
            {
                Id,
                CorrelationId,
                CreatedAt,
                TransactionType,
                NumeroContaCredito,
                Valor,
                Descricao,
                Referencia,
                Observacoes,
                Metadata
            };

            return System.Text.Json.JsonSerializer.Serialize(data);
        }
    }

}
