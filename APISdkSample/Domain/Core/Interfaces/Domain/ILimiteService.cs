using bks.sdk.Common.Results;
using Domain.Core.Enums;
using Domain.Core.Models;


namespace Domain.Core.Interfaces.Domain
{
    public interface ILimiteService
    {


        ValueTask<ValidationResult> ValidarLimiteDebitoAsync(string numeroConta, decimal valor, CancellationToken cancellationToken = default);
        ValueTask AtualizarLimiteUtilizadoAsync(string numeroConta, decimal valor, TipoLimite tipo, CancellationToken cancellationToken = default);
        ValueTask<LimiteInfo> ObterLimitesContaAsync(string numeroConta, CancellationToken cancellationToken = default);
        ValueTask<bool> RedefinirLimitesAsync(string numeroConta, CancellationToken cancellationToken = default);
    }
}
