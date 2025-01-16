using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Domain.Args
{
    public class AttachmentProgressUpdatedEventArgs : EventArgs
    {
        public AttachmentProgressUpdatedEventArgs(AttachmentProgress progress, Guid attachmentGroupId)
        {
            Progress = progress;
            AttachmentGroupId = attachmentGroupId;
        }

        public AttachmentProgress Progress { get; set; }
        public Guid AttachmentGroupId { get; set; }
    }
}
