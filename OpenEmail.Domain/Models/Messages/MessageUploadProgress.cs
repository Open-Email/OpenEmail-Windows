using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenEmail.Domain.Models.Messages
{

    public partial class MessageUploadProgress : ObservableObject
    {
        public Guid MessageId { get; set; }
        public string UploadTitle { get; set; }

        [ObservableProperty]
        private MessageStatus _status;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Progress))]
        [NotifyPropertyChangedFor(nameof(IsProgressAvailable))]
        private int _totalParts;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Progress))]
        private int _currentPart;

        public int Progress => TotalParts > 0 ? (int)((double)((CurrentPart * 100) / TotalParts)) : 0;

        public bool IsProgressAvailable => TotalParts > 1;

        protected MessageUploadProgress() { }

        public static MessageUploadProgress CreateNew(Guid messageId, string uploadTitle, int totalParts)
        {
            return new MessageUploadProgress()
            {
                MessageId = messageId,
                UploadTitle = uploadTitle,
                Status = MessageStatus.Uploading,
                TotalParts = totalParts,
            };
        }
    }
}
