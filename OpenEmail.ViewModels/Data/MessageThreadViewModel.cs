using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.ViewModels.Data
{
    public partial class MessageThreadViewModel : ObservableObject, IMessageViewModel
    {
        public string SubjectId { get; set; }
        private ObservableCollection<MessageViewModel> _messages = new ObservableCollection<MessageViewModel>();

        public ReadOnlyObservableCollection<MessageViewModel> Messages { get; }

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isSelected;

        #region IMessageViewModel implementation

        public ObservableCollection<ContactViewModel> ReaderViewModels => ((IMessageViewModel)VisibleMessage).ReaderViewModels;

        public ObservableCollection<AttachmentViewModel> AttachmentViewModels => ((IMessageViewModel)VisibleMessage).AttachmentViewModels;

        public ContactViewModel ContactViewModel => ((IMessageViewModel)VisibleMessage).ContactViewModel;

        public bool IsRead { get => ((IMessageViewModel)VisibleMessage).IsRead; set => ((IMessageViewModel)VisibleMessage).IsRead = value; }
        public string Author { get => ((IMessageViewModel)VisibleMessage).Author; set => ((IMessageViewModel)VisibleMessage).Author = value; }
        public string Body { get => ((IMessageViewModel)VisibleMessage).Body; set => ((IMessageViewModel)VisibleMessage).Body = value; }
        public DateTimeOffset CreatedAt { get => ((IMessageViewModel)VisibleMessage).CreatedAt; set => ((IMessageViewModel)VisibleMessage).CreatedAt = value; }

        public bool HasAttachments => ((IMessageViewModel)VisibleMessage).HasAttachments;

        public Message Message => ((IMessageViewModel)VisibleMessage).Message;

        public string Subject { get => ((IMessageViewModel)VisibleMessage).Subject; set => ((IMessageViewModel)VisibleMessage).Subject = value; }

        public bool IsBroadcast => ((IMessageViewModel)VisibleMessage).IsBroadcast;

        public bool SubjectIdMatches(string subjectId) => VisibleMessage.SubjectIdMatches(subjectId);

        #endregion

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ReaderViewModels))]
        [NotifyPropertyChangedFor(nameof(AttachmentViewModels))]
        [NotifyPropertyChangedFor(nameof(ContactViewModel))]
        [NotifyPropertyChangedFor(nameof(IsRead))]
        [NotifyPropertyChangedFor(nameof(Author))]
        [NotifyPropertyChangedFor(nameof(Body))]
        [NotifyPropertyChangedFor(nameof(CreatedAt))]
        [NotifyPropertyChangedFor(nameof(HasAttachments))]
        [NotifyPropertyChangedFor(nameof(Message))]
        [NotifyPropertyChangedFor(nameof(Subject))]
        public MessageViewModel visibleMessage;

        public MessageThreadViewModel(string subjectId)
        {
            SubjectId = subjectId;

            Messages = new ReadOnlyObservableCollection<MessageViewModel>(_messages);
        }

        public void AddMessage(MessageViewModel message)
        {
            // Message doesn't belong to thread.
            if (message.SubjectId != SubjectId) return;

            // Find the correct position to insert the message to maintain order
            int index = FindInsertionIndex(message);
            _messages.Insert(index, message);

            // Last received item is the visible message
            VisibleMessage = Messages[0];
        }

        #region Helpers

        private int FindInsertionIndex(MessageViewModel newMessage)
        {
            var comparer = new MessageComparer();

            for (int i = 0; i < Messages.Count; i++)
            {
                if (comparer.Compare(Messages[i], newMessage) > 0)
                {
                    return i;
                }
            }

            return Messages.Count; // Insert at the end if no earlier message is found
        }

        public bool HasAttachmentGroupId(Guid attachmentGroupId)
        {
            return ((IMessageViewModel)VisibleMessage).HasAttachmentGroupId(attachmentGroupId);
        }

        #endregion
    }
}
