using bks.sdk.Common.Results;
using bks.sdk.Processing.Abstractions;
using bks.sdk.Processing.Mediator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bks.sdk.Processing.Mediator;

public class BKSMediatorProcessor<TRequest, TResponse> : IMediatorProcessor<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IMediator _mediator;

    public string ProcessorName => "MediatorProcessor";

    public BKSMediatorProcessor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<TResponse>> ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return await _mediator.SendAsync(request, cancellationToken);
    }
}
