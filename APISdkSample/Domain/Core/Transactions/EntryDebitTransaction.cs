using bks.sdk.Transactions;

namespace Domain.Core.Transactions
{
    public record EntryDebitTransaction : BaseTransaction
    {
        public int Branch { get; init; } 
        public string AccountNumber { get; init; } = string.Empty;
        public decimal Value { get; init; }
        public string Detail { get; init; } = string.Empty;
        public string? Ref { get; init; }

    }
}
