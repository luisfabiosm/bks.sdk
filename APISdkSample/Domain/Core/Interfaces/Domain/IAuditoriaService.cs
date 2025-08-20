namespace Domain.Core.Interfaces.Domain
{
    public interface IAuditoriaService
    {
        ValueTask RegistrarInicioOperacaoAsync(
            string idOperacao,
            string tipoOperacao,
            decimal valor,
            string contaOrigem,
            string contaDestino,
            CancellationToken cancellationToken);
    }
}
