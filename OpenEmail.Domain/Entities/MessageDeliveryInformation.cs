using SQLite;

namespace OpenEmail.Domain.Entities
{
    public class MessageDeliveryInformation
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string EnvelopeId { get; set; }
        public string Link { get; set; }
        public DateTimeOffset? SeenAt { get; set; }
    }
}
