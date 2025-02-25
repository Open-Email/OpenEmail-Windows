using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels.Data;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.ViewModels
{
    public partial class ComposerPageViewModel : BaseViewModel
    {
        public event EventHandler DismissWindow;

        private SemaphoreSlim _draftUpdateSemaphore = new(1);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsErrorMessageVisible))]
        private string _errorMessage;

        [ObservableProperty]
        private ObservableCollection<ContactViewModel> contactViewModels = new();

        public bool IsErrorMessageVisible => !string.IsNullOrEmpty(ErrorMessage);

        private IMessage _referencingMessage;
        private MailActionType _actionType;

        private readonly IContactService _contactService;
        private readonly IFileService _fileService;
        private readonly IMessagesService _messagesService;
        private readonly IAttachmentManager _attachmentManager;
        private readonly IProfileDataService _profileDataService;
        private readonly IDialogService _dialogService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IMessagePreperationService _messagePreperationService;

        [ObservableProperty]
        private MessageViewModel _draftMessageViewModel;

        public ComposerPageViewModel(IContactService contactService,
                                     IFileService fileService,
                                     IMessagesService messagesService,
                                     IAttachmentManager attachmentManager,
                                     IProfileDataService profileDataService,
                                     IDialogService dialogService,
                                     IApplicationStateService applicationStateService,
                                     IMessagePreperationService messagePreperationService)
        {
            _contactService = contactService;
            _fileService = fileService;
            _messagesService = messagesService;
            _attachmentManager = attachmentManager;
            _profileDataService = profileDataService;
            _dialogService = dialogService;
            _applicationStateService = applicationStateService;
            _messagePreperationService = messagePreperationService;
        }

        [RelayCommand]
        private async Task SendAsync()
        {
            HideError();

            if (string.IsNullOrWhiteSpace(DraftMessageViewModel.Body))
            {
                ShowErrorMessage("Please enter a message.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DraftMessageViewModel.Subject))
            {
                ShowErrorMessage("Please enter a subject.");
                return;
            }

            // Root message withour readers.
            if (!DraftMessageViewModel.IsBroadcast && !DraftMessageViewModel.ReaderViewModels.Any())
            {
                ShowErrorMessage("Please add at least one reader to the message.");
                return;
            }

            await AutoSaveLocalDraftAsync();

            // Make sure all readers are accessable by the author.

            List<string> failedProfileList = new();
            List<ReaderUploadData> readerMap = new();

            // Validate profiles and create reader map.
            foreach (var reader in DraftMessageViewModel.ReaderViewModels)
            {
                try
                {
                    reader.Profile ??= await _profileDataService.GetProfileDataAsync(UserAddress.CreateFromAddress(reader.Contact.Address));

                    if (reader.Profile == null || (string.IsNullOrEmpty(reader.Profile.EncryptionKey) || string.IsNullOrEmpty(reader.Profile.SigningKey)))
                    {
                        failedProfileList.Add(reader.Contact.Address);
                        continue;
                    }
                }
                catch (Exception)
                {
                    failedProfileList.Add(reader.Contact.Address);
                    continue;
                }

                readerMap.Add(new ReaderUploadData(reader.Contact.Address, reader.Profile.EncryptionKey, reader.Profile.SigningKey, reader.Profile.EncryptionKeyId));
            }

            // Can't continue with these readers.
            if (failedProfileList.Any())
            {
                ShowErrorMessage($"The following addresses are not valid: {string.Join(", ", failedProfileList)}");

                return;
            }

            Messenger.Send(new SendMessage(DraftMessageViewModel.Id, readerMap));
            DismissWindow?.Invoke(this, EventArgs.Empty);
        }

        private void ShowErrorMessage(string message) => ErrorMessage = message;

        private void HideError() => ErrorMessage = string.Empty;

        [RelayCommand]
        private async Task RemoveAttachment(AttachmentViewModel attachmentViewModel)
        {
            if (attachmentViewModel.AttachmentParts == null || !attachmentViewModel.AttachmentParts.Any()) return;

            var groupId = attachmentViewModel.AttachmentParts.First().AttachmentGroupId;

            DraftMessageViewModel.AttachmentViewModels.Remove(attachmentViewModel);

            await _messagesService.RemoveMessageAttachmentAsync(groupId);
            await AutoSaveLocalDraftAsync();
        }


        [RelayCommand]
        private async Task BrowseAsync()
        {
            var files = await _fileService.PickFilesAsync(WindowType.Composer);

            if (files.Count == 0) return;

            // Check if the files are there.
            // We can't add same file name twice.

            foreach (var filePath in files)
            {
                await AddAttachmentAsync(filePath);
            }
        }

        public async Task AddAttachmentAsync(string filePath)
        {
            var existingAttachment = DraftMessageViewModel.AttachmentViewModels.FirstOrDefault(a => a.FileName == Path.GetFileName(filePath));

            if (existingAttachment == null)
            {
                var attachmentPartTuples = _messagesService.CreateMessageAttachments(DraftMessageViewModel.Self, filePath);

                foreach (var part in attachmentPartTuples)
                {
                    // Save them to the disk.

                    var messageAttachment = part.Item1;
                    var fileData = part.Item2;

                    await _attachmentManager.SaveAttachmentEnvelopeAsync(messageAttachment, fileData);
                    await _messagesService.SaveMessageAttachmentAsync(messageAttachment);
                }

                var parts = attachmentPartTuples.Select(a => a.Item1).ToList();

                var attachmentViewModel = new AttachmentViewModel(parts, DraftMessageViewModel.Self);
                DraftMessageViewModel.AttachmentViewModels.Add(attachmentViewModel);

                GC.Collect();
            }
        }

        public async Task AddReaderAsync(string address)
        {
            // Can't add myself or empty or existing.
            if (string.IsNullOrWhiteSpace(address) ||
                address == _applicationStateService.ActiveProfile.Account.Address.FullAddress ||
                DraftMessageViewModel.ReaderViewModels.Any(a => a.Contact.Address == address)) return;

            var profileData = await _messagePreperationService.PrepareContactViewModel(address);
            DraftMessageViewModel.ReaderViewModels.Add(profileData);
        }

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            if (parameter is ComposeWindowArgs args)
            {
                _actionType = args.Type;
                _referencingMessage = args.ReferencingMessage;
            }

            // Load contacts

            var allContacts = await _contactService.GetContactsAsync(_applicationStateService.ActiveProfile.Account.Id);

            foreach (var contact in allContacts)
            {
                var profileData = await _messagePreperationService.PrepareContactViewModel(contact.Address);
                ContactViewModels.Add(profileData);
            }

            DraftMessageViewModel = _referencingMessage as MessageViewModel;

            DraftMessageViewModel.AttachmentViewModels.CollectionChanged += AttachmentsUpdated;
            DraftMessageViewModel.ReaderViewModels.CollectionChanged += ReadersUpdated;
        }

        private async void ReadersUpdated(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => await AutoSaveLocalDraftAsync();

        private async void AttachmentsUpdated(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            => await AutoSaveLocalDraftAsync();

        public override async void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedFrom(navigationMode, parameter);

            DraftMessageViewModel.AttachmentViewModels.CollectionChanged -= AttachmentsUpdated;
            DraftMessageViewModel.ReaderViewModels.CollectionChanged -= ReadersUpdated;

            await AutoSaveLocalDraftAsync();
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (DraftMessageViewModel == null) return;

            await _messagesService.DeleteMessageToTrashAsync(DraftMessageViewModel.Id);

            DismissWindow?.Invoke(this, EventArgs.Empty);
        }

        private async Task AutoSaveLocalDraftAsync()
        {
            if (DraftMessageViewModel == null) return;

            try
            {
                await _draftUpdateSemaphore.WaitAsync();

                DraftMessageViewModel.Size = DraftMessageViewModel.Body.Length;
                DraftMessageViewModel.Category = "personal";
                DraftMessageViewModel.ReceivedAt = DateTimeOffset.Now;
                DraftMessageViewModel.Readers = string.Join(",", DraftMessageViewModel.ReaderViewModels.Select(a => a.Contact.Address));

                Debug.WriteLine($"Draft is auto-saved.");

                await _messagesService.UpdateMessageAsync(DraftMessageViewModel.Self).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto saving draft failed: {ex.Message}");
            }
            finally
            {
                _draftUpdateSemaphore.Release();
            }
        }
    }
}
