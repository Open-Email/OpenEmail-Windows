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
        private string forwardQuoteMessageFormat = @"

----------
Forwarded message from: {0}

{1}";

        private string replyQuoteMessageFormat = @""
            + "\n\n"
            + "On {0}, {1} wrote: \n\n"
            + "{2}";

        public event EventHandler<string> RenderMessage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTrashFolder))]
        [NotifyPropertyChangedFor(nameof(IsOutboxFolder))]
        private MailFolder _currentMailFolderType;

        [ObservableProperty]
        private ObservableCollection<MessageViewModel> _selectedMessages = new();

        public MessageViewModel SelectedSingleMessage => SelectedMessages.FirstOrDefault();

        public bool IsTrashFolder => CurrentMailFolderType == MailFolder.Trash;
        public bool IsOutboxFolder => CurrentMailFolderType == MailFolder.Outbox;
        public bool IsBroadcastMessage => HasSelectedSingleMessage && SelectedSingleMessage.IsBroadcast;

        public bool IsDraftMessage => HasSelectedSingleMessage && SelectedSingleMessage.IsDraft;
        public bool IsNotDraftMessage => !IsDraftMessage;

        public bool HasMultipleMessagesSelected => SelectedMessages.Count > 1;
        public bool HasSelectedSingleMessage => SelectedMessages.Count == 1;
        public bool HasNoSelectedMessage => SelectedMessages.Count == 0;
        public bool HasNoMessages => Messages.Count == 0;
        public bool HasMultipleReaders => HasSelectedSingleMessage && SelectedSingleMessage.ReaderViewModels.Count > 1;
        public bool ShouldDisplayReplyAll => HasMultipleReaders && IsNotDraftMessage;

        public ObservableCollection<MessageViewModel> Messages { get; set; } = [];
        public IPreferencesService PreferencesService { get; }

        private readonly IDialogService _dialogService;
        private readonly IMessagesService _messagesService;
        private readonly IMessagePreperationService _messagePreperationService;
        private Expression<Func<Message, bool>> filter;

        // When draft is created, it's the message id to be selected for new composer window.
        private Guid? messageToComposeId;

        public MailListPageViewModel(IFileService fileService,
                                     IApplicationStateService applicationStateService,
                                     IPreferencesService preferencesService,
                                     IDialogService dialogService,
                                     IMessagesService messagesService,
                                     IMessagePreperationService messagePreperationService) : base(fileService, applicationStateService)
        {
            PreferencesService = preferencesService;
            _dialogService = dialogService;
            _messagesService = messagesService;
            _messagePreperationService = messagePreperationService;
        }

        partial void OnCurrentMailFolderTypeChanging(MailFolder oldValue, MailFolder newValue)
        {
            SelectedMessages.Clear();
        }

        private async void OnSelectedMessagesUpdated(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSelectedSingleMessage));
            OnPropertyChanged(nameof(HasNoSelectedMessage));
            OnPropertyChanged(nameof(IsDraftMessage));
            OnPropertyChanged(nameof(IsBroadcastMessage));
            OnPropertyChanged(nameof(IsNotDraftMessage));
            OnPropertyChanged(nameof(HasMultipleMessagesSelected));
            OnPropertyChanged(nameof(HasMultipleReaders));
            OnPropertyChanged(nameof(SelectedSingleMessage));
            OnPropertyChanged(nameof(IsTrashFolder));
            OnPropertyChanged(nameof(IsOutboxFolder));

            DeletePermanentlyCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            RecallAndDeleteCommand.NotifyCanExecuteChanged();

            // Mark items as read if they were not.
            foreach (var message in SelectedMessages)
            {
                if (!message.IsRead)
                {
                    await _messagesService.MarkMessageReadAsync(message.Message.Id).ConfigureAwait(false);
                    Dispatcher.ExecuteOnDispatcher(() => message.IsRead = true);
                }
            }

            if (HasSelectedSingleMessage)
            {
                RenderMessage?.Invoke(this, SelectedSingleMessage.Message.Body);
            }
        }

        [RelayCommand]
        private void Forward()
        {
            if (SelectedSingleMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.Forward, SelectedSingleMessage)));
        }

        [RelayCommand]
        private void Reply()
        {
            if (SelectedSingleMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.Reply, SelectedSingleMessage)));
        }

        [RelayCommand]
        private void ReplyAll()
        {
            if (SelectedSingleMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.ReplyAll, SelectedSingleMessage)));
        }

        public async void Receive(ListingFolderChanged message)
        {
            CurrentMailFolderType = message.NewFolder;

            await InitializeDataAsync();
        }

        [RelayCommand]
        private void EditDraftMessage()
        {
            if (SelectedSingleMessage?.Message == null) return;

            Messenger.Send(new NewComposeRequested(new ComposeWindowArgs(MailActionType.EditDraft, SelectedSingleMessage)));
        }

        [RelayCommand]
        public void DisplaySender(AccountContact contact)
            => Messenger.Send(new ProfileDisplayRequested(contact));

        public override void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedFrom(navigationMode, parameter);

            SelectedMessages.CollectionChanged -= OnSelectedMessagesUpdated;
        }

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            SelectedMessages.CollectionChanged += OnSelectedMessagesUpdated;
            Messages.CollectionChanged += OnMessagesUpdated;

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

        private void OnMessagesUpdated(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasNoMessages));
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
                    // Reply: Add the author as reader.

                    if (args.Type == MailActionType.Reply)
                    {
                        message.Readers = args.ReferencingMessage.Author;
                        message.Body = GetInitialReplyMessageFormat(args.ReferencingMessage.Author, args.ReferencingMessage.ReceivedAt, args.ReferencingMessage.Body);
                    }
                    else if (args.Type == MailActionType.ReplyAll)
                    {
                        // Reply all: All readers except myself + author.
                        var readers = args.ReferencingMessage.Readers.Split(',').ToList();

                        readers.Add(args.ReferencingMessage.Author);

                        var readerString = string.Join(",", readers.Distinct());

                        message.Readers = readerString;
                        message.Body = GetInitialReplyMessageFormat(args.ReferencingMessage.Author, args.ReferencingMessage.ReceivedAt, args.ReferencingMessage.Body);
                    }
                    else if (args.Type == MailActionType.Forward)
                    {
                        message.Body = GetInitialForwardMessageFormat(args.ReferencingMessage.Author, args.ReferencingMessage.Body);

                        if (args.ReferencingMessage is MessageViewModel messageViewModel && messageViewModel.HasAttachments)
                        {
                            // Create the attachments for forwarding message.
                            foreach (var attachment in messageViewModel.AttachmentViewModels)
                            {
                                var attachmentParts = _messagesService.CreateMessageAttachmentMetadata(message, attachment.LocalFilePath);

                                if (attachmentParts != null)
                                {
                                    foreach (var part in attachmentParts)
                                    {
                                        // Save them to the disk.
                                        await _messagesService.SaveMessageAttachmentAsync(part);
                                    }
                                }
                            }
                        }
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

        private string GetInitialForwardMessageFormat(string referenceAuthor, string referenceContent)
            => string.Format(forwardQuoteMessageFormat, referenceAuthor, referenceContent);

        private string GetInitialReplyMessageFormat(string referenceAuthor, DateTimeOffset referenceReplyDate, string referenceContent)
            => string.Format(replyQuoteMessageFormat, referenceReplyDate.ToString("yyyy-MM-dd HH:mm:ss"), referenceAuthor, referenceContent);

        public override async Task InitializeDataAsync(CancellationToken cancellationToken = default)
        {
            DetachAttachmentProgresses();

            Messages.Clear();

            filter = a => a.AccountId == ApplicationStateService.ActiveProfile.Account.Id;

            // Add more conditions to filter based on the current folder type.
            if (CurrentMailFolderType == MailFolder.Trash)
                filter = filter.AndAlso(a => a.IsDeleted);
            else if (CurrentMailFolderType == MailFolder.Drafts)
                filter = filter.AndAlso(a => a.IsDraft && !a.IsDeleted);
            else if (CurrentMailFolderType == MailFolder.Outbox)
                filter = filter.AndAlso(a => a.Author == ApplicationStateService.ActiveProfile.Address && !a.IsDeleted && !a.IsDraft);
            else if (CurrentMailFolderType == MailFolder.Broadcast)
                filter = filter.AndAlso(a => a.IsBroadcast && !a.IsDeleted);
            else
                filter = filter.AndAlso(a => !a.IsDeleted && !a.IsDraft && !a.IsBroadcast && a.Author != ApplicationStateService.ActiveProfile.Address);

            var messages = await _messagesService.GetMessagesAsync(filter);

            foreach (var message in messages)
            {
                var messageViewModel = await _messagePreperationService.PrepareViewModelAsync(message, Dispatcher, CurrentMailFolderType == MailFolder.Outbox, cancellationToken);

                Messages.Add(messageViewModel);
            }
        }

        public override void OnUpdateAttachmentViewModelProgress(Guid attachmentGroupId, AttachmentProgress attachmentProgress)
        {
            // Only update for selected message.
            if (SelectedSingleMessage == null || !SelectedSingleMessage.HasAttachmentGroupId(attachmentGroupId)) return;

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

                    if (SelectedMessages.FirstOrDefault(a => a.Id == model.Id) is MessageViewModel selectedVariant)
                    {
                        SelectedMessages.Remove(selectedVariant);
                    }
                });
            }
        }

        public async void Receive(MessageAdded message)
        {
            // Check if message falls to existing filter first.

            if (!filter.Compile().Invoke(message.Message)) return;

            var messageViewModel = await _messagePreperationService.PrepareViewModelAsync(message.Message, Dispatcher, CurrentMailFolderType == MailFolder.Outbox);

            // TODO: Add sorted.
            // TODO: Grouping by conversation id.

            ExecuteUIThread(() =>
            {
                Messages.Add(messageViewModel);

                if (messageToComposeId != null)
                {
                    var composingMessage = Messages.FirstOrDefault(m => m.Message.Id == messageToComposeId);

                    if (composingMessage == null) return;

                    // Clear existing selection and select the new message.
                    SelectedMessages.Clear();
                    SelectedMessages.Add(composingMessage);

                    Messenger.Send(new DraftComposeArgs(MailActionType.New, SelectedSingleMessage));

                    messageToComposeId = null;
                }
            });
        }

        public async void Receive(ComposeWindowArgs message) => await ManageComposeAsync(message);

        [RelayCommand(CanExecute = nameof(IsTrashFolder))]
        private async Task DeletePermanentlyAsync()
        {
            if (SelectedMessages.Count == 0) return;

            var messageIdsToDelete = SelectedMessages.Select(a => a.Id).ToArray();

            foreach (var messageId in messageIdsToDelete)
            {
                await _messagesService.DeleteMessagePermanentAsync(messageId);

            }
        }

        [RelayCommand(CanExecute = nameof(IsOutboxFolder))]
        private async Task RecallAndDeleteAsync()
        {
            if (SelectedMessages.Count == 0) return;

            var messageIdsToDelete = SelectedMessages.Select(a => a.Id).ToArray();

            foreach (var messageId in messageIdsToDelete)
            {
                await _messagesService.RecallMessageAsync(messageId);
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (IsTrashFolder)
            {
                await DeletePermanentlyAsync();
            }
            else if (IsOutboxFolder)
            {
                await RecallAndDeleteAsync();
            }
            else
            {
                // Move items to trash.
                if (SelectedMessages.Count == 0) return;

                var messageIdsToDelete = SelectedMessages.Select(a => a.Id).ToArray();

                foreach (var messageId in messageIdsToDelete)
                {
                    await _messagesService.DeleteMessageToTrashAsync(messageId);
                }
            }
        }
    }
}
