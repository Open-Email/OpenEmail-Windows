using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when user wants to download an attachment.
    /// </summary>
    /// <param name="Info">Attachment information for downloading and saving data.</param>
    public record StartAttachmentDownload(AttachmentDownloadInfo Info);
}
