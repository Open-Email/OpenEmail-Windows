using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
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
    public partial class BroadcastPageViewModel : MessageBasedPageViewModel
    {
        public ObservableCollection<BroadcastMessageViewModel> BroadcastMessages { get; } = [];

        public bool HasNoBroadcastMessages => BroadcastMessages.Count == 0;

        private readonly IApplicationStateService _applicationStateService;
        private readonly IMessagesService _messagesService;
        private readonly IMessagePreperationService _messagePreperationService;

        public BroadcastPageViewModel(IFileService fileService,
                                      IApplicationStateService applicationStateService,
                                      IMessagesService messagesService,
                                      IMessagePreperationService messagePreperationService) : base(fileService, applicationStateService)
        {
            _applicationStateService = applicationStateService;
            _messagesService = messagesService;
            _messagePreperationService = messagePreperationService;
        }

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);
            BroadcastMessages.CollectionChanged += MessagesChanged;

            await InitializeDataAsync();
        }

        public override void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedFrom(navigationMode, parameter);
            BroadcastMessages.CollectionChanged -= MessagesChanged;
        }

        [RelayCommand]
        private async Task CreateBroadcastAsync()
        {
            var args = new ComposeWindowArgs(MailActionType.Broadcast);
            Messenger.Send(new NewComposeRequested(args));
        }

        [RelayCommand]
        private void ToggleMessageExpansion(BroadcastMessageViewModel message)
        {
            message.IsExpanded = !message.IsExpanded;
        }

        private void MessagesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasNoBroadcastMessages));
        }

        public override void DetachAttachmentProgresses()
        {
            var attachments = BroadcastMessages.SelectMany(m => m.AttachmentViewModels);

            foreach (var attachment in attachments)
            {
                attachment.UnhookProgress();
            }
        }

        public override async Task InitializeDataAsync(CancellationToken cancellationToken = default)
        {
            DetachAttachmentProgresses();

            BroadcastMessages.Clear();

            var broadcastMessages = await _messagesService
                .GetMessagesAsync(a => a.AccountId == _applicationStateService.ActiveProfile.Account.Id && a.IsBroadcast && !a.IsDeleted);

            foreach (var message in broadcastMessages)
            {
                var messageViewModel = await _messagePreperationService.PrepareViewModelAsync(message, Dispatcher, cancellationToken);

                BroadcastMessages.Add(new BroadcastMessageViewModel(messageViewModel.Message, messageViewModel.ContactViewModel, messageViewModel.ReaderViewModels, messageViewModel.AttachmentViewModels));
            }
        }

        public override void OnUpdateAttachmentViewModelProgress(Guid attachmentGroupId, AttachmentProgress attachmentProgress)
        {
            // Find the attachment to attach progress onto.
            var messageViewModel = BroadcastMessages
                .FirstOrDefault(a => a.HasAttachments && a.AttachmentViewModels.Any(a => a.AttachmentParts[0].AttachmentGroupId == attachmentGroupId));

            if (messageViewModel == null) return;

            var attachmentViewModel = messageViewModel.AttachmentViewModels.FirstOrDefault(a => a.AttachmentParts[0].AttachmentGroupId == attachmentGroupId);

            if (attachmentViewModel == null) return;

            // Hook created progress to the view model.
            attachmentViewModel.HookProgress(attachmentProgress, Dispatcher);
        }

        public override void OnProfileDataUpdated(UserAddress userAddress, ProfileData profileData)
        {
            foreach (var message in BroadcastMessages)
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
    }
}
