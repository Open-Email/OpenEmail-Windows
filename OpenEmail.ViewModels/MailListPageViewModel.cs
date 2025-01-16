using System.Collections.ObjectModel;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Extensions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels.Data;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.ViewModels
{
    public partial class MailListPageViewModel : MessageBasedPageViewModel,
        IRecipient<ListingFolderChanged>,
        IRecipient<MessageDeleted>,
        IRecipient<MessageAdded>,
        IRecipient<ComposeWindowArgs>
    {
        [ObservableProperty]
        private MailFolder _currentMailFolderType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedMessage))]
        [NotifyPropertyChangedFor(nameof(HasNoSelectedMessage))]
        [NotifyPropertyChangedFor(nameof(IsDraftMessage))]
        [NotifyPropertyChangedFor(nameof(IsNotDraftMessage))]
        private MessageViewModel _selectedMessage;

        public bool IsDraftMessage => SelectedMessage?.Message.IsDraft ?? false;
        public bool IsNotDraftMessage => !IsDraftMessage;

        public bool HasSelectedMessage => SelectedMessage != null;
        public bool HasNoSelectedMessage => !HasSelectedMessage;

        public bool HasMultipleReaders => SelectedMessage == null ? false : SelectedMessage.ReaderViewModels.Count > 1;
        public bool ShouldDisplayReplyAll => HasMultipleReaders && IsNotDraftMessage;

        public ObservableCollection<MessageViewModel> Messages { get; set; } = [];
        public IPreferencesService PreferencesService { get; }

        private readonly IMessagesService _messagesService;
        private readonly IMessagePreperationService _messagePreperationService;
        private Expression<Func<Message, bool>> filter;

        // When draft is created, it's the message id to be selected for new composer window.
        private Guid? messageToComposeId;

        public MailListPageViewModel(IFileService fileService,
                                     IApplicationStateService applicationStateService,
                                     IPreferencesService preferencesService,
                                     IMessagesService messagesService,
                                     IMessagePreperationService messagePreperationService) : base(fileService, applicationStateService)
        {
            PreferencesService = preferencesService;
            _messagesService = messagesService;
            _messagePreperationService = messagePreperationService;
        }

        [RelayCommand]
        private void Forward()
        {
            if (SelectedMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.Forward, SelectedMessage)));
        }

        [RelayCommand]
        private void Reply()
        {
            if (SelectedMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.Reply, SelectedMessage)));
        }

        [RelayCommand]
        private void ReplyAll()
        {
            if (SelectedMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.ReplyAll, SelectedMessage)));
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (SelectedMessage?.Message == null) return;

            await _messagesService.DeleteMessageAsync(SelectedMessage.Message.Id);
        }

        public async void Receive(ListingFolderChanged message)
        {
            CurrentMailFolderType = message.NewFolder;

            await InitializeDataAsync();
        }

        [RelayCommand]
        private void EditDraftMessage()
        {
            if (SelectedMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.EditDraft, SelectedMessage)));
        }

        [RelayCommand]
        public void DisplaySender(AccountContact contact)
            => Messenger.Send(new ProfileDisplayRequested(contact));

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            if (parameter is MailFolder folderType)
            {
                CurrentMailFolderType = folderType;

                await InitializeDataAsync();
            }
            else if (parameter is ComposeWindowArgs composingArgs)
            {
                CurrentMailFolderType = MailFolder.Drafts;

                await InitializeDataAsync();
                await ManageComposeAsync(composingArgs);
            }
        }

        private async Task ManageComposeAsync(ComposeWindowArgs composingArgs)
        {
            var draftMessageViewModel = await GetDraftMessageViewModelAsync(composingArgs);

            if (draftMessageViewModel != null)
            {
                Messenger.Send(new DraftComposeArgs(composingArgs.Type, draftMessageViewModel));
            }
        }

        private async Task<MessageViewModel> GetDraftMessageViewModelAsync(ComposeWindowArgs args)
        {
            if (args.Type != MailActionType.EditDraft)
            {
                // Create draft message.

                var message = new Message
                {
                    AccountId = ApplicationStateService.ActiveProfile.Account.Id,
                    Author = ApplicationStateService.ActiveProfile.Account.Address.FullAddress,
                    Id = Guid.NewGuid(),
                    IsDraft = true,
                    IsRead = true,
                    Body = string.Empty,
                    CreatedAt = DateTimeOffset.Now,
                    EnvelopeId = CryptoUtils.GenerateMessageId(ApplicationStateService.ActiveProfile.UserAddress),
                    IsBroadcast = args.Type == MailActionType.Broadcast,
                };

                // Assign referencing message properties.
                if (args.ReferencingMessage != null)
                {
                    // IsBroadcastVisible = false;

                    // Reply: Add the author as reader.

                    if (args.Type == MailActionType.Reply)
                    {
                        message.Readers = args.ReferencingMessage.Author;
                    }
                    else if (args.Type == MailActionType.ReplyAll)
                    {
                        // Reply all: All readers except myself + author.
                        var readers = args.ReferencingMessage.Readers.Split(',').ToList();

                        readers.Add(args.ReferencingMessage.Author);

                        var readerString = string.Join(",", readers.Distinct());

                        message.Readers = readerString;
                    }

                    // Apply subject-id.
                    message.SubjectId = args.ReferencingMessage.EnvelopeId;
                }

                // Save the message.
                messageToComposeId = message.Id;

                await _messagesService.UpdateMessageAsync(message);
            }

            // Find the message to edit.
            if (args.ReferencingMessage != null)
            {
                return Messages.FirstOrDefault(m => m.Message.Id == args.ReferencingMessage.Id);
            }

            return null;
        }

        public override async Task InitializeDataAsync(CancellationToken cancellationToken = default)
        {
            DetachAttachmentProgresses();

            Messages.Clear();

            filter = a => a.AccountId == ApplicationStateService.ActiveProfile.Account.Id;

            // Add more conditions to filter based on the current folder type.
            if (CurrentMailFolderType == MailFolder.Trash)
                filter = filter.AndAlso(a => a.IsDeleted);
            else if (CurrentMailFolderType == MailFolder.Drafts)
                filter = filter.AndAlso(a => a.IsDraft);
            else if (CurrentMailFolderType == MailFolder.Outbox)
                filter = filter.AndAlso(a => a.Author == ApplicationStateService.ActiveProfile.Address && !a.IsDeleted && !a.IsDraft);
            else
                filter = filter.AndAlso(a => !a.IsDeleted && !a.IsDraft && a.Author != ApplicationStateService.ActiveProfile.Address);

            var messages = await _messagesService.GetMessagesAsync(filter);

            foreach (var message in messages)
            {
                var messageViewModel = await _messagePreperationService.PrepareViewModelAsync(message, Dispatcher, cancellationToken);

                Messages.Add(messageViewModel);
            }
        }

        partial void OnSelectedMessageChanged(MessageViewModel value)
        {
            if (value == null) return;

            if (!value.IsRead)
            {
                _messagesService.MarkMessageReadAsync(value.Message.Id).ConfigureAwait(false);

                Dispatcher.ExecuteOnDispatcher(() => value.IsRead = true);
            }
        }

        public override void OnUpdateAttachmentViewModelProgress(Guid attachmentGroupId, AttachmentProgress attachmentProgress)
        {
            // Only update for selected message.
            if (SelectedMessage == null || !SelectedMessage.HasAttachmentGroupId(attachmentGroupId)) return;

            // Find the attachment to attach progress onto.
            var messageViewModel = Messages
                .FirstOrDefault(a => a.HasAttachments && a.AttachmentViewModels.Any(a => a.AttachmentParts[0].AttachmentGroupId == attachmentGroupId));

            if (messageViewModel == null) return;

            var attachmentViewModel = messageViewModel.AttachmentViewModels.FirstOrDefault(a => a.AttachmentParts[0].AttachmentGroupId == attachmentGroupId);

            if (attachmentViewModel == null) return;

            // Hook created progress to the view model.
            attachmentViewModel.HookProgress(attachmentProgress, Dispatcher);
        }

        public override void OnProfileDataUpdated(UserAddress userAddress, ProfileData profileData)
        {
            foreach (var message in Messages)
            {
                if (message.ContactViewModel.Contact.Address == userAddress.FullAddress)
                {
                    message.ContactViewModel.Profile = profileData;
                }

                foreach (var reader in message.ReaderViewModels)
                {
                    if (reader.Contact.Address == userAddress.FullAddress)
                    {
                        reader.Profile = profileData;
                    }
                }
            }
        }

        public override void DetachAttachmentProgresses()
        {
            var attachments = Messages.SelectMany(m => m.AttachmentViewModels);

            foreach (var attachment in attachments)
            {
                attachment.UnhookProgress();
            }
        }

        public void Receive(MessageDeleted message)
        {
            var messageViewModels = Messages.Where(m => m.Message.Id == message.Message.Id);

            foreach (var model in messageViewModels)
            {
                ExecuteUIThread(() =>
                {
                    Messages.Remove(model);
                });
            }
        }

        public async void Receive(MessageAdded message)
        {
            // Check if message falls to existing filter first.

            if (!filter.Compile().Invoke(message.Message)) return;

            var messageViewModel = await _messagePreperationService.PrepareViewModelAsync(message.Message, Dispatcher);

            // TODO: Add sorted.
            // TODO: Grouping by conversation id.

            ExecuteUIThread(() =>
            {
                Messages.Add(messageViewModel);

                if (messageToComposeId != null)
                {
                    SelectedMessage = Messages.FirstOrDefault(m => m.Message.Id == messageToComposeId);

                    if (SelectedMessage != null)
                    {
                        Messenger.Send(new DraftComposeArgs(MailActionType.New, SelectedMessage));
                    }

                    messageToComposeId = null;
                }
            });
        }

        public async void Receive(ComposeWindowArgs message)
        {
            await ManageComposeAsync(message);
        }
    }
}
