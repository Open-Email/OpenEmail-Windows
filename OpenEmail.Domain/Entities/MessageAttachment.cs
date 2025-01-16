using SQLite;

namespace OpenEmail.Domain.Entities
{
    public class MessageAttachment
    {
        [PrimaryKey]
        public string Id { get; set; }
        public Guid AttachmentGroupId { get; set; }
        public string ParentId { get; set; } // Id of the root message for the attachment.
        public long Size { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public string MimeType { get; set; }
        public string FileName { get; set; }
        public int Part { get; set; }
        public string AccessKey { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Id) &&
                   !string.IsNullOrEmpty(ParentId) &&
                   Size > 0 &&
                   !string.IsNullOrEmpty(FileName) &&
                   Part > 0 &&
                   !string.IsNullOrEmpty(AccessKey);
        }
    }
}
