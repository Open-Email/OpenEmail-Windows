using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// When attachment manager creates a download session for the given attachment group.
    /// </summary>
    /// <param name="AttachmentGroupId">Attachment group id.</param>
    /// <param name="Progress">Progress model.</param>
    public record AttachmentDownloadSessionCreated(Guid AttachmentGroupId, AttachmentProgress Progress);
}
