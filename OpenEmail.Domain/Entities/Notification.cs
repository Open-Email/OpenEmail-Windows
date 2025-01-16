using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Notifications;
using SQLite;

namespace OpenEmail.Domain.Entities
{
    public record Notification
    {
        [PrimaryKey]
        public string Id { get; init; }
        public DateTimeOffset ReceivedAt { get; init; }
        public string Link { get; init; }

        /// <summary>
        /// Decrypted notifier address.
        /// </summary>
        public string Address { get; init; }
        public string AuthorFingerPrint { get; init; }

        public bool IsExpired() => ReceivedAt < DateTimeOffset.UtcNow.AddDays(-7);
        public UserAddress UserAddress => UserAddress.CreateFromAddress(Address);

        public Notification(NotificationResponse response)
        {
            Id = response.Id;
            ReceivedAt = DateTimeOffset.UtcNow;
            Link = response.Link;
            Address = response.DecryptedNotifierAddress;
            AuthorFingerPrint = response.NotifierSigningKeyFingerprint;
        }

        public Notification() { }
    }
}
