using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Notifications;
using SQLite;

namespace OpenEmail.Core.DataServices
{
    public class NotificationsService : BaseDataService, INotificationsService
    {
        private readonly IClientFactory _clientFactory;

        public NotificationsService(IDatabaseService<ISQLiteAsyncConnection> databaseService,
                                    IClientFactory clientFactory) : base(databaseService)
        {
            _clientFactory = clientFactory;
        }

        public async Task<List<NotificationResponse>> FetchNewNotificationsAsync(string address)
        {
            var newNotifications = new List<NotificationResponse>();

            var client = _clientFactory.CreateProfileClient<INotificationsClient>();

            var res = await client.GetNotificationsAsync(UserAddress.CreateFromAddress(address));

            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();

            var splittedNotifications = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // Compare remote notifications with local notifications and return only new ones.
            foreach (var notification in splittedNotifications)
            {
                var splittedNotification = notification.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                var notificationResponse = new NotificationResponse(splittedNotification[0],
                                                                    splittedNotification[1],
                                                                    splittedNotification[2],
                                                                    splittedNotification[3]);

                // Don't add the notification if it already exists.
                bool exists = await IsNotificationExistsAsync(notificationResponse);

                if (exists) continue;

                var newNotification = new Notification(notificationResponse);
                newNotifications.Add(notificationResponse);
            }

            return newNotifications;
        }

        private async Task<bool> IsNotificationExistsAsync(NotificationResponse notificationResponse)
        {
            return await Connection.Table<Notification>().CountAsync(a => a.Id == notificationResponse.Id) > 0;
        }

        public async Task InsertRangeAsync(IEnumerable<Notification> notifications)
            => await Connection.InsertAllAsync(notifications).ConfigureAwait(false);

        public Task InsertNotificationAsync(Notification notification)
            => Connection.InsertAsync(notification);

        public Task<Notification> GetNotificationAsync(string id)
            => Connection.Table<Notification>().Where(a => a.Id == id).FirstOrDefaultAsync();
    }
}
