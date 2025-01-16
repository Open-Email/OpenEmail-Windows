using SQLite;

namespace OpenEmail.Domain.Entities
{
    public record AccountContact
    {
        [PrimaryKey]
        public Guid UniqueId { get; set; }
        public string Id { get; set; }
        public Guid AccountId { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRequestAcccepted { get; set; }
        public bool ReceiveBroadcasts { get; set; }
    }
}
