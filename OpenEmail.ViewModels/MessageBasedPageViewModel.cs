using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels
{
    /// <summary>
    /// Shared view model base for pages that operates on messages.
    /// Code sharing for broadcast and message list pages.
    /// </summary>
    public abstract partial class MessageBasedPageViewModel : BaseViewModel,
        IRecipient<AttachmentDownloadSessionCreated>,
        IRecipient<ProfileDataUpdated>,
        IRecipient<ProfileDataCreated>
    {
        private readonly IFileService _fileService;
        protected IApplicationStateService ApplicationStateService { get; }

        public MessageBasedPageViewModel(IFileService fileService, IApplicationStateService applicationStateService)
        {
            _fileService = fileService;
            ApplicationStateService = applicationStateService;
        }

        [RelayCommand]
        private async Task OpenAttachmentAsync(AttachmentViewModel attachmentViewModel)
        {
            if (!attachmentViewModel.LocallyExists)
            {
                var downloadInfo = attachmentViewModel.CreateDownloadInfo(ApplicationStateService.ActiveProfile);
                var downloadRequest = new StartAttachmentDownload(downloadInfo, true);

                Messenger.Send(downloadRequest);
                return;
            }

            await _fileService.LaunchFileAsync(attachmentViewModel.LocalFilePath);
        }

        [RelayCommand]
        private async Task SaveAttachmentAsync(AttachmentViewModel attachmentViewModel)
        {
            var pickedFolder = await _fileService.PickFolderAsync();

            if (string.IsNullOrEmpty(pickedFolder)) return;

            if (!attachmentViewModel.LocallyExists)
            {
                var downloadInfo = attachmentViewModel.CreateDownloadInfo(ApplicationStateService.ActiveProfile);
                var downloadRequest = new StartAttachmentDownload(downloadInfo, false, pickedFolder);

                Messenger.Send(downloadRequest);
                return;
            }
            else
            {
                // No need to download.
                var destinationPath = Path.Combine(pickedFolder, attachmentViewModel.FileName);

                File.Copy(attachmentViewModel.LocalFilePath, destinationPath, true);
            }
        }

        public void Receive(AttachmentDownloadSessionCreated message)
            => OnUpdateAttachmentViewModelProgress(message.AttachmentGroupId, message.Progress);

        public void Receive(ProfileDataUpdated message) => OnProfileDataUpdated(message.UserAddress, message.ProfileData);
        public void Receive(ProfileDataCreated message) => OnProfileDataUpdated(message.UserAddress, message.ProfileData);

        public virtual void OnUpdateAttachmentViewModelProgress(Guid attachmentGroupId, AttachmentProgress attachmentProgress) { }
        public virtual void OnProfileDataUpdated(UserAddress userAddress, ProfileData profileData) { }

        public abstract void DetachAttachmentProgresses();
        public abstract Task InitializeDataAsync(CancellationToken cancellationToken = default);

        public override void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedFrom(navigationMode, parameter);

            DetachAttachmentProgresses();
        }
    }
}
