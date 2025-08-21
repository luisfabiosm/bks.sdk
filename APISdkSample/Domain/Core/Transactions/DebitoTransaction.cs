using bks.sdk.Transactions;

namespace Domain.Core.Transactions
{
    public record DebitoTransaction : BaseTransaction
    {
        public int AgenciaConta { get; init; } = 0;
        public string NumeroConta { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
    }

}
