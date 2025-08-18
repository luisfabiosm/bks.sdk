namespace bks.sdk.Transactions;

public interface ITransactionProcessor
{
    Task<bks.sdk.Common.Results.Result> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default);
}