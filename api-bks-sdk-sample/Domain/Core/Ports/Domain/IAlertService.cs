using Domain.Core.Enums;

namespace Domain.Core.Ports.Domain
{
    public interface IAlertService
    {
        //Task<IEnumerable<object>> GetAlertHistoryAsync(object value, DateTime startDate,  CancellationToken cancellationToken);

        Task SendAlertAsync(string title, string message, AlertPriority priority, CancellationToken cancellationToken);
        Task SendCriticalAlertAsync(string title, string message, CancellationToken cancellationToken);
    }
}
