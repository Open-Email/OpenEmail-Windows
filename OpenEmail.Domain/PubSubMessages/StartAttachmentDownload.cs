using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when user wants to download an attachment.
    /// </summary>
    /// <param name="Info">Attachment information for downloading and saving data.</param>
    /// <param name="LaunchAfter">Whether the file must be launched after download finishes.</param>
    /// <param name="SaveAfterPath">Folder path to save the downloaded file after download finishes.</param>
    public record StartAttachmentDownload(AttachmentDownloadInfo Info, bool LaunchAfter = false, string SaveAfterPath = "");
}
