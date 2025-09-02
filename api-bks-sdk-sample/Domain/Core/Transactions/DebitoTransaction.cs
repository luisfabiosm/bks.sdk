using Microsoft.AspNetCore.Http.HttpResults;

namespace Domain.Core.Transactions
{
    public record DebitoTransaction : BaseTransaction
    {
        public int NumeroConta { get; init; }
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string? Referencia { get; init; }
        public bool PermitirSaldoNegativo { get; init; } = false;
        public decimal? LimiteOperacao { get; init; }

        // Regras de negócio específicas da transação de débito
        public bool IsValorValido => Valor > 0 && Valor <= (LimiteOperacao ?? 100_000); // Limite padrão de 100 mil
        public bool IsContaValida => !(NumeroConta==0);
        public bool IsDescricaoValida => !string.IsNullOrWhiteSpace(Descricao) && Descricao.Length <= 200;

        // Verificar se a transação está dentro do horário comercial (se necessário)
        public bool IsHorarioComercial
        {
            get
            {
                var agora = DateTime.Now;
                return agora.Hour >= 8 && agora.Hour <= 18 &&
                       agora.DayOfWeek != DayOfWeek.Saturday &&
                       agora.DayOfWeek != DayOfWeek.Sunday;
            }
        }

        public override string Serialize()
        {
            var data = new
            {
                Id,
                CorrelationId,
                CreatedAt,
                TransactionType,
                NumeroConta,
                Valor,
                Descricao,
                Referencia,
                PermitirSaldoNegativo,
                LimiteOperacao,
                Metadata
            };

            return System.Text.Json.JsonSerializer.Serialize(data);
        }
    }

}
