using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Notifications;

namespace OpenEmail.Contracts.Services
{
    public interface INotificationsService
    {
        Task<Notification> GetNotificationAsync(string id);
        Task InsertRangeAsync(IEnumerable<Notification> notifications);
        Task<List<NotificationResponse>> FetchNewNotificationsAsync(string address);
        Task InsertNotificationAsync(Notification notification);
    }
}
