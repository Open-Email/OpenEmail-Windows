using OpenEmail.Domain.Models.Messages;
using SQLite;

namespace OpenEmail.Domain.Entities
{
    public class Message : IMessage
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string EnvelopeId { get; set; }
        public string Author { get; set; }
        public Guid AccountId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
        public MessageStatus Status { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string SubjectId { get; set; } // Thread id.
        public string Body { get; set; }
        public string StreamId { get; set; }
        public long Size { get; set; }
        public bool IsBroadcast { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsDraft { get; set; }
        public string Readers { get; set; } // Comma seperated addresses.
        public string AccessKey { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        [Ignore]
        public Message Self => this;

    }
}
