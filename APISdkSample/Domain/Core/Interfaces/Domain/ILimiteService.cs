using bks.sdk.Common.Results;
using Domain.Core.Enums;
using Domain.Core.Models;


namespace Domain.Core.Interfaces.Domain
{
    public interface ILimiteService
    {


        ValueTask<ValidationResult> ValidarLimiteDebitoAsync(int numeroConta, decimal valor, CancellationToken cancellationToken = default);
        ValueTask AtualizarLimiteUtilizadoAsync(int numeroConta, decimal valor, TipoLimite tipo, CancellationToken cancellationToken = default);
        ValueTask<LimiteInfo> ObterLimitesContaAsync(int numeroConta, CancellationToken cancellationToken = default);
        ValueTask<bool> RedefinirLimitesAsync(int numeroConta, CancellationToken cancellationToken = default);
    }
}
