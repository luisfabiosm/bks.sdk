using bks.sdk.Common.Results;
using bks.sdk.Core.Configuration;
using bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Pipeline.Steps.bks.sdk.Core.Extensions.bks.sdk.Core.Initialization;
using bks.sdk.Observability.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Core.Pipeline.Steps;

public class ProcessingStep<TRequest, TResponse> : BasePipelineStep<TRequest, TResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BKSFrameworkSettings _settings;
    private readonly IBKSLogger _logger;

    public override string StepName => "Processing";
    public override int Order => 3;

    public ProcessingStep(
        IServiceProvider serviceProvider,
        BKSFrameworkSettings settings,
        IBKSLogger logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
    }

    public override async Task<Result<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await OnStepStarting(request);

        try
        {
            _logger.Trace($"Iniciando processamento - Modo: {_settings.Processing.Mode}");

            Result<TResponse> result;

            if (_settings.Processing.Mode == ProcessingMode.Mediator)
            {
                result = await ProcessViaMediatorAsync(request, cancellationToken);
            }
            else
            {
                result = await ProcessViaTransactionProcessorAsync(request, cancellationToken);
            }

            await OnStepCompleted(request, result);
            return result;
        }
        catch (Exception ex)
        {
            await OnStepFailed(request, ex);
            throw;
        }
    }

    private async Task<Result<TResponse>> ProcessViaMediatorAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var processor = _serviceProvider.GetService<IMediatorProcessor<TRequest, TResponse>>();

        if (processor == null)
        {
            return Result<TResponse>.Failure($"Nenhum processador Mediator encontrado para {typeof(TRequest).Name}");
        }

        return await processor.ProcessAsync(request, cancellationToken);
    }

    private async Task<Result<TResponse>> ProcessViaTransactionProcessorAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var processor = _serviceProvider.GetService<ITransactionProcessor<TRequest, TResponse>>();

        if (processor == null)
        {
            return Result<TResponse>.Failure($"Nenhum processador de transação encontrado para {typeof(TRequest).Name}");
        }

        return await processor.ProcessAsync(request, cancellationToken);
    }
}

