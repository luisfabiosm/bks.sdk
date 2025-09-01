using bks.sdk.Common.Enums;
using bks.sdk.Processing.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Factories;


public interface IProcessorFactory
{
    IBKSBusinessProcessor<TRequest, TResponse>? GetProcessor<TRequest, TResponse>(ProcessingMode mode)
        where TRequest : class;
}