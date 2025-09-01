using bks.sdk.Processing.Transactions.Abstractions;
using Domain.Core.Models.Entities;
using Domain.Core.Transactions;

namespace Domain.Core.Interfaces.Outbound
{
    public interface INotificationAdapter
    {
        ValueTask EnviarNotificacaoAsync(BaseTransaction transaction, CancellationToken cancellationToken);
    }
}
