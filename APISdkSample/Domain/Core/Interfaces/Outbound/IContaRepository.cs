using bks.sdk.Common.Results;

namespace Domain.Core.Interfaces.Outbound
{
    public interface IContaRepository
    {
        Task<Conta> GetByNumeroAsync(string numeroConta, CancellationToken cancellationToken);

         Task<ReservaResult> ReservarSaldoAsync(string numeroConta, decimal valor, string correlationId, CancellationToken cancellationToken);

        Task<Result> ConfirmarTransacaoAsync(string numeroConta, decimal valor, string correlationId, CancellationToken cancellationToken);

        Task<Result> ReverterReservaAsync(string numeroConta, decimal valor, string correlationId, CancellationToken cancellationToken);
    }

   
}
