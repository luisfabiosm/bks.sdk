using bks.sdk.Common.Results;
using Domain.Core.Models.Entities;

namespace Domain.Core.Interfaces.Outbound
{
    public interface IContaRepository
    {
        ValueTask<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        ValueTask<Conta?> GetByNumeroAsync(string numero, CancellationToken cancellationToken = default);
        ValueTask UpdateAsync(Conta conta, CancellationToken cancellationToken = default);
        ValueTask<bool> ExistsAsync(string numero, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<MovimentacaoInfo>> GetMovimentacoesAsync(string contaId, DateTime? dataInicio = null, DateTime? dataFim = null, CancellationToken cancellationToken = default);
    }

   
}
