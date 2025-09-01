using bks.sdk.Common.Results;
using bks.sdk.Observability.Logging;
using bks.sdk.Processing.Mediator.Abstractions;


namespace bks.sdk.Processing.Mediator;

public class BKSMediator : IBKSMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBKSLogger _logger;

    public BKSMediator(IServiceProvider serviceProvider, IBKSLogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result<TResponse>> SendAsync<TResponse>(
        IBKSRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return Result<TResponse>.Failure("Request não pode ser nulo");
        }

        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var handlerType = typeof(IBKSRequestHandler<,>).MakeGenericType(requestType, responseType);

        _logger.Trace($"Procurando handler para: {requestType.Name} -> {responseType.Name}");

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            var error = $"Nenhum handler encontrado para {requestType.Name} -> {responseType.Name}";
            _logger.Error(error);
            return Result<TResponse>.Failure(error);
        }

        try
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
            {
                return Result<TResponse>.Failure("Método HandleAsync não encontrado no handler");
            }

            var task = (Task<Result<TResponse>>)method.Invoke(handler, new object[] { request, cancellationToken })!;
            return await task;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao executar handler: {ex.Message}");
            return Result<TResponse>.Failure($"Erro na execução do handler: {ex.Message}");
        }
    }
}
