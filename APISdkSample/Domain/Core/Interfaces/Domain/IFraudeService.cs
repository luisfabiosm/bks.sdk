using bks.sdk.Transactions;
using Domain.Core.Models;
using Domain.Core.Models.Results;
using Domain.Core.Transactions;

namespace Domain.Core.Interfaces.Domain
{
    public interface IFraudeService
    {

        ValueTask<AnaliseRisco> AnalisarTransacaoAsync(BaseTransaction transacao, CancellationToken cancellationToken = default);
        ValueTask<PerfilRisco> ObterPerfilRiscoContaAsync(int numeroConta, CancellationToken cancellationToken = default);
        ValueTask RegistrarEventoSuspeitoAsync(string transacaoId, string motivo, CancellationToken cancellationToken = default);

    }
}
