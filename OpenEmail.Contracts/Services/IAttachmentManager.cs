using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Contracts.Services
{
    public interface IAttachmentManager
    {
        Task<byte[]> GetAttachmentAsync(List<MessageAttachment> allPartsOfAttachment);
        Task SaveAttachmentEnvelopeAsync(MessageAttachment attachment, byte[] content);
        Task StartDownloadAttachmentAsync(AttachmentDownloadInfo downloadInfo);
        AttachmentProgress GetAttachmentProgress(AttachmentDownloadInfo attachmentDownloadInfo);
        string CreateAttachmentFilePath(string parentId, string fileName);
        void DeleteAttachment(MessageAttachment messageAttachment);
        byte[] GetAttachmentChunkBytes(MessageAttachment messageAttachment);
    }
}
