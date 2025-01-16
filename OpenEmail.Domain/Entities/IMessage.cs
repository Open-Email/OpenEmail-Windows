using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Domain.Entities
{
    public interface IMessage
    {
        string AccessKey { get; set; }
        Guid AccountId { get; set; }
        string Author { get; set; }
        string Body { get; set; }
        string Category { get; set; }
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset? DeletedAt { get; set; }
        string EnvelopeId { get; set; }
        Guid Id { get; set; }
        bool IsBroadcast { get; set; }
        bool IsDeleted { get; set; }
        bool IsDraft { get; set; }
        bool IsRead { get; set; }
        string Readers { get; set; }
        DateTimeOffset ReceivedAt { get; set; }
        long Size { get; set; }
        MessageStatus Status { get; set; }
        string StreamId { get; set; }
        string Subject { get; set; }
        string SubjectId { get; set; }
        Message Self { get; }
    }
}
