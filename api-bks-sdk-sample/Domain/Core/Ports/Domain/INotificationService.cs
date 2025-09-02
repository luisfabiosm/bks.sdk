using Domain.Core.Entities;

namespace Domain.Core.Ports.Domain
{
    public interface INotificationService
    {
        Task NotifyTransactionCompletedAsync(Conta conta, string transactionType, DateTime completedAt, CancellationToken cancellationToken);
    }

}
