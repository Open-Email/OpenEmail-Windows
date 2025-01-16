using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.ViewModels.Data
{
    public partial class MessageViewModel : ObservableObject, IMessage
    {
        [ObservableProperty]
        public Message _message;

        public ContactViewModel ContactViewModel { get; }
        public ObservableCollection<ContactViewModel> ReaderViewModels { get; } = [];
        public ObservableCollection<AttachmentViewModel> AttachmentViewModels { get; } = [];

        public bool HasAttachments => AttachmentViewModels?.Count > 0;
        public int AttachmentCount => AttachmentViewModels?.Count ?? 0;

        public bool IsRead
        {
            get => Message.IsRead;
            set => SetProperty(Message.IsRead, value, Message, (m, r) => m.IsRead = r);
        }

        public string AccessKey
        {
            get => Message.AccessKey;
            set => SetProperty(Message.AccessKey, value, Message, (m, r) => m.AccessKey = r);
        }

        public Guid AccountId
        {
            get => Message.AccountId;
            set => SetProperty(Message.AccountId, value, Message, (m, r) => m.AccountId = r);
        }

        public string Author
        {
            get => Message.Author;
            set => SetProperty(Message.Author, value, Message, (m, r) => m.Author = r);
        }

        public string Body
        {
            get => Message.Body;
            set => SetProperty(Message.Body, value, Message, (m, r) => m.Body = r);
        }

        public string Category
        {
            get => Message.Category;
            set => SetProperty(Message.Category, value, Message, (m, r) => m.Category = r);
        }

        public DateTimeOffset CreatedAt
        {
            get => Message.CreatedAt;
            set => SetProperty(Message.CreatedAt, value, Message, (m, r) => m.CreatedAt = r);
        }

        public DateTimeOffset? DeletedAt
        {
            get => Message.DeletedAt;
            set => SetProperty(Message.DeletedAt, value, Message, (m, r) => m.DeletedAt = r);
        }

        public string EnvelopeId
        {
            get => Message.EnvelopeId;
            set => SetProperty(Message.EnvelopeId, value, Message, (m, r) => m.EnvelopeId = r);
        }

        public Guid Id
        {
            get => Message.Id;
            set => SetProperty(Message.Id, value, Message, (m, r) => m.Id = r);
        }

        public bool IsBroadcast
        {
            get => Message.IsBroadcast;
            set => SetProperty(Message.IsBroadcast, value, Message, (m, r) => m.IsBroadcast = r);
        }

        public bool IsDeleted
        {
            get => Message.IsDeleted;
            set => SetProperty(Message.IsDeleted, value, Message, (m, r) => m.IsDeleted = r);
        }

        public bool IsDraft
        {
            get => Message.IsDraft;
            set => SetProperty(Message.IsDraft, value, Message, (m, r) => m.IsDraft = r);
        }

        public string Readers
        {
            get => Message.Readers;
            set => SetProperty(Message.Readers, value, Message, (m, r) => m.Readers = r);
        }

        public DateTimeOffset ReceivedAt
        {
            get => Message.ReceivedAt;
            set => SetProperty(Message.ReceivedAt, value, Message, (m, r) => m.ReceivedAt = r);
        }

        public long Size
        {
            get => Message.Size;
            set => SetProperty(Message.Size, value, Message, (m, r) => m.Size = r);
        }

        public MessageStatus Status
        {
            get => Message.Status;
            set => SetProperty(Message.Status, value, Message, (m, r) => m.Status = r);
        }

        public string StreamId
        {
            get => Message.StreamId;
            set => SetProperty(Message.StreamId, value, Message, (m, r) => m.StreamId = r);
        }

        public string Subject
        {
            get => Message.Subject;
            set => SetProperty(Message.Subject, value, Message, (m, r) => m.Subject = r);
        }

        public string SubjectId
        {
            get => Message.SubjectId;
            set => SetProperty(Message.SubjectId, value, Message, (m, r) => m.SubjectId = r);
        }

        public Message Self => Message;

        public MessageViewModel(Message message, ContactViewModel contactViewModel, IEnumerable<ContactViewModel> readers, IEnumerable<AttachmentViewModel> attachmentViewModels = null)
        {
            UpdateMessage(message, attachmentViewModels, readers);

            ContactViewModel = contactViewModel;
        }

        public bool HasAttachmentGroupId(Guid attachmentGroupId)
        {
            return AttachmentViewModels.Any(a => a.AttachmentParts.Any(b => b.AttachmentGroupId == attachmentGroupId));
        }

        public void UpdateMessage(Message message, IEnumerable<AttachmentViewModel> newAttachments, IEnumerable<ContactViewModel> newReaders)
        {
            Message = message;
            OnPropertyChanged(nameof(IsRead));

            if (newAttachments != null)
            {
                AttachmentViewModels.Clear();

                foreach (var attachment in newAttachments)
                {
                    AttachmentViewModels.Add(attachment);
                }

                OnPropertyChanged(nameof(AttachmentCount));
            }

            if (newReaders != null)
            {
                ReaderViewModels.Clear();

                foreach (var reader in newReaders)
                {
                    ReaderViewModels.Add(reader);
                }
            }
        }
    }
}
