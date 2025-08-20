using bks.sdk.Transactions;
using Domain.Core.Enums;

namespace Domain.Core.Transactions
{
    public record TransferenciaTransaction : BaseTransaction
    {
        public string NumeroContaOrigem { get; init; } = string.Empty;
        public string NumeroContaDestino { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public TipoTransferencia Tipo { get; init; }
        public decimal Taxa { get; private set; }

        public void SetTaxa(decimal taxa)
        {
            Taxa = taxa;
        }
    }
}
