using Domain.Core.Entities;

namespace Domain.Core.Ports.Outbound
{
    public interface IContaRepository
    {
        Task<Conta?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Conta?> GetByNumeroAsync(int numero, CancellationToken cancellationToken = default);
        Task<IEnumerable<Conta>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Conta>> GetByTitularAsync(string titular, CancellationToken cancellationToken = default);
        Task CreateAsync(Conta conta, CancellationToken cancellationToken = default);
        Task UpdateAsync(Conta conta, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int numero, CancellationToken cancellationToken = default);
        Task<IEnumerable<Movimentacao>> GetMovimentacoesAsync(string contaId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Movimentacao>> GetMovimentacoesPeriodoAsync(string contaId, DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
    }


}
