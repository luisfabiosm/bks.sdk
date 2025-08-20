using Domain.Core.Models.Results;
using Domain.Core.Transactions;

namespace Domain.Core.Interfaces.Domain
{
    public interface IAntiFraudeService
    {

        ValueTask<AnaliseDeTransferenciaResult> AnalisarTransferenciaAsync(
                                                TransferenciaTransaction transferencia,
                                                CancellationToken cancellationToken);

    }
}
