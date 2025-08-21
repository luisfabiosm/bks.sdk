using bks.sdk.Common.Results;

namespace bks.sdk.Transactions;

public interface ITransactionProcessor
{
    Task<bks.sdk.Common.Results.Result> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default);
}

public interface ITransactionProcessor<TResult>
{
    Task<Result<TResult>> ExecuteAsync(BaseTransaction transaction, CancellationToken cancellationToken = default);
    bool CanProcess(BaseTransaction transaction);
}