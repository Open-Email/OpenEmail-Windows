using System.Collections.ObjectModel;
using OpenEmail.Domain.Entities;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels.Interfaces
{
    public interface IMessageViewModel : ISelectableItem
    {
        ObservableCollection<ContactViewModel> ReaderViewModels { get; }
        ObservableCollection<AttachmentViewModel> AttachmentViewModels { get; }
        ContactViewModel ContactViewModel { get; }
        bool IsRead { get; set; }
        string Author { get; set; }
        string Body { get; set; }
        DateTimeOffset CreatedAt { get; set; }
        bool HasAttachments { get; }
        Message Message { get; }
        bool IsBroadcast { get; }
        string Subject { get; set; }
        string SubjectId { get; set; }

        bool HasAttachmentGroupId(Guid attachmentGroupId);
        bool SubjectIdMatches(string subjectId);
    }
}
