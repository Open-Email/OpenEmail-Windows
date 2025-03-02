using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.Models.Messages
{
    public record AttachmentDownloadInfo(List<MessageAttachment> Parts, AccountProfile Profile, AccountLink Link, UserAddress TargetAddress)
    {
        public Guid AttachmentGroupId { get; } = Parts[0].AttachmentGroupId;
        public string EnvelopeId { get; } = Parts[0].ParentId;
        public string FileName { get; } = Parts[0].FileName;
    }
}
