using Domain.Core.Enums;

namespace Domain.Core.Interfaces.Domain
{
    public interface ITaxaService
    {

        ValueTask<decimal> CalcularTaxaAsync(
                           TipoTransferencia tipoTransacao,
                           decimal valorTransacao,
                           CancellationToken cancellationToken);
    }
}
