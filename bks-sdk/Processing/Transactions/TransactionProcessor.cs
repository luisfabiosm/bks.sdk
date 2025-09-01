using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Processing.Abstractions;
using bks.sdk.Processing.Transactions.Processors;

namespace bks.sdk.Processing.Transactions;

public class TransactionProcessor<TTransaction, TResponse> : IBKSTransactionProcessor<TTransaction, TResponse>
    where TTransaction : BaseTransaction
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBKSLogger _logger;

    public string ProcessorName => "TransactionProcessor";

    public TransactionProcessor(IServiceProvider serviceProvider, IBKSLogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result<TResponse>> ProcessAsync(TTransaction request, CancellationToken cancellationToken = default)
    {
        var transactionType = request.GetType();
        var responseType = typeof(TResponse);
        var processorType = typeof(BaseTransactionProcessor<,>).MakeGenericType(transactionType, responseType);

        _logger.Trace($"Procurando processador para: {transactionType.Name} -> {responseType.Name}");

        var processor = _serviceProvider.GetService(processorType);
        if (processor == null)
        {
            var error = $"Nenhum processador de transação encontrado para {transactionType.Name} -> {responseType.Name}";
            _logger.Error(error);
            return Result<TResponse>.Failure(error);
        }

        try
        {
            var method = processorType.GetMethod("ProcessAsync");
            if (method == null)
            {
                return Result<TResponse>.Failure("Método ProcessAsync não encontrado no processador");
            }

            var task = (Task<Result<TResponse>>)method.Invoke(processor, new object[] { request, cancellationToken })!;
            return await task;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao executar processador de transação: {ex.Message}");
            return Result<TResponse>.Failure($"Erro na execução do processador: {ex.Message}");
        }
    }
}
