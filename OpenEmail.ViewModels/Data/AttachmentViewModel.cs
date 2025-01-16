using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Messages;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.ViewModels.Data
{
    public partial class AttachmentViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProgress))]
        private int _progress;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProgress))]
        [NotifyPropertyChangedFor(nameof(CanOpen))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanDownload))]
        private AttachmentDownloadStatus _status;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LocallyExists))]
        [NotifyPropertyChangedFor(nameof(CanOpen))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanDownload))]
        private string _localFilePath;

        public bool LocallyExists => File.Exists(LocalFilePath);

        public bool CanOpen => LocallyExists;
        public bool CanSave => LocallyExists;
        public bool CanDownload => !LocallyExists && Status != AttachmentDownloadStatus.Downloading;

        public bool HasProgress => Status == AttachmentDownloadStatus.Downloading || (Progress > 0 && Progress < 100);

        public string FileName { get; }
        public long FileSize { get; }
        public List<MessageAttachment> AttachmentParts { get; }
        public Message ParentMessage { get; }
        public AttachmentProgress AttachmentProgress { get; private set; }

        private IPlatformDispatcher _dispatcher;

        public string AttachmentThumbnailImage
        {
            get
            {
                var fileExtension = Path.GetExtension(FileName).ToLowerInvariant();

                if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".gif")
                {
                    return $"ms-appx:///Assets/img.png";
                }
                else if (fileExtension == ".mp4" || fileExtension == ".mov" || fileExtension == ".avi" || fileExtension == ".mkv")
                {
                    return $"ms-appx:///Assets/video.png";
                }
                else if (fileExtension == ".mp3" || fileExtension == ".wav" || fileExtension == ".flac" || fileExtension == ".ogg")
                {
                    return $"ms-appx:///Assets/audio.png";
                }
                else if (fileExtension == ".pdf")
                {
                    return $"ms-appx:///Assets/pdf.png";
                }
                else if (fileExtension == ".exe")
                {
                    return $"ms-appx:///Assets/exe.png";
                }
                else if (fileExtension == ".zip" || fileExtension == ".rar" || fileExtension == ".7z")
                {
                    return $"ms-appx:///Assets/archive.png";
                }
                else
                {
                    return $"ms-appx:///Assets/file.png";
                }
            }
        }

        public AttachmentViewModel(List<MessageAttachment> attachmentParts, Message parentMessage)
        {
            AttachmentParts = attachmentParts;
            ParentMessage = parentMessage;

            FileName = attachmentParts[0].FileName;
            FileSize = attachmentParts.Sum(a => a.Size);
        }

        private void UpdateProgressValues()
        {
            _dispatcher.ExecuteOnDispatcher(() =>
            {
                Progress = AttachmentProgress.Progress;
                Status = AttachmentProgress.Status;
            });
        }

        public void HookProgress(AttachmentProgress progress, IPlatformDispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);

            _dispatcher = dispatcher;

            AttachmentProgress = progress;

            UpdateProgressValues();

            AttachmentProgress.ProgressChanged -= InternalProgressChanged;
            AttachmentProgress.ProgressChanged += InternalProgressChanged;
        }

        private void InternalProgressChanged(object sender, EventArgs e) => UpdateProgressValues();

        public void UnhookProgress()
        {
            if (AttachmentProgress == null) return;

            AttachmentProgress.ProgressChanged -= InternalProgressChanged;
            _dispatcher = null;
        }

        public AttachmentDownloadInfo CreateDownloadInfo(AccountProfile forProfile)
        {
            // Reciever
            var ownerAddress = UserAddress.CreateFromAddress(forProfile.Address);

            // Sender
            var targetAddress = UserAddress.CreateFromAddress(ParentMessage.Author);

            var link = AccountLink.Create(ownerAddress, targetAddress);
            return new AttachmentDownloadInfo(AttachmentParts, forProfile, link, targetAddress);
        }
    }
}
