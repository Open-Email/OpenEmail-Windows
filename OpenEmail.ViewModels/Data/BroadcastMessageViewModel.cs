using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Domain.Entities;

namespace OpenEmail.ViewModels.Data
{
    public partial class BroadcastMessageViewModel : MessageViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExpansionButtonText))]
        private bool _isExpanded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldShowExpansionButton))]
        private bool _isTrimmed;

        public string ExpansionButtonText => IsExpanded ? "Show Less" : "Show More";
        public bool ShouldShowExpansionButton => IsTrimmed || HasAttachments;

        public BroadcastMessageViewModel(Message message, ContactViewModel contactViewModel, ObservableCollection<ContactViewModel> readers, ObservableCollection<AttachmentViewModel> attachmentViewModels = null)
            : base(message, contactViewModel, readers, attachmentViewModels)
        {
        }
    }
}
