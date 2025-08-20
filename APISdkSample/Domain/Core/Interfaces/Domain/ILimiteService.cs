using Domain.Core.Enums;
using Domain.Core.Models.Results;

namespace Domain.Core.Interfaces.Domain
{
    public interface ILimiteService
    {

        ValueTask<LimiteResult> VerificarLimitesAsync(
                        string numeroContaOrigem,
                        decimal valor,
                        TipoTransferencia tipo,
                        CancellationToken cancellationToken);


    }
}
