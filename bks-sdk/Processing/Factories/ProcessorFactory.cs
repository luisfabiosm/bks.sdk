using bks.sdk.Common.Enums;
using bks.sdk.Processing.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Factories;


public class ProcessorFactory : IProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBusinessProcessor<TRequest, TResponse>? GetProcessor<TRequest, TResponse>(ProcessingMode mode)
        where TRequest : class
    {
        return mode switch
        {
            ProcessingMode.Mediator => _serviceProvider.GetService<IMediatorProcessor<TRequest, TResponse>>(),
            ProcessingMode.TransactionProcessor => _serviceProvider.GetService<ITransactionProcessor<TRequest, TResponse>>(),
            _ => null
        };
    }
}
