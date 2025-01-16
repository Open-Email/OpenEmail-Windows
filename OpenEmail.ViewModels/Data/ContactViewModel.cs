using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Contacts;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.ViewModels.Data
{
    public partial class ContactViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProfileThumbnailUrl))]
        [NotifyPropertyChangedFor(nameof(ProfileImageUrl))]
        [NotifyPropertyChangedFor(nameof(Initials))]
        private ProfileData _profile;

        public string ProfileImageUrl => $"{Contact.Address}.png";
        public string ProfileThumbnailUrl => $"{Contact.Address}_thumbnail.png";
        public string Initials => GetInitials();

        [ObservableProperty]
        private ContactRelationship relationship;

        [ObservableProperty]
        private AccountContact _contact;
        private readonly bool _isSelf;

        public string ContactDisplayName
        {
            get
            {
                if (_isSelf) return "Me";

                // Profile -> Contnact -> Address
                if (!string.IsNullOrWhiteSpace(Profile?.Name)) return Profile.Name;

                if (!string.IsNullOrEmpty(Contact?.Name)) return Contact.Name;

                return Contact?.Address;
            }
        }

        public ContactViewModel(AccountContact contact, ProfileData profileData, bool isSelf = false)
        {
            _isSelf = isSelf;

            Contact = contact;
            Profile = profileData;
        }

        public string GetInitials()
        {
            string initialsValue = string.Empty;

            if (!string.IsNullOrEmpty(Profile?.Name))
            {
                initialsValue = Profile.Name;
            }
            else if (!string.IsNullOrEmpty(Contact?.Name))
            {
                initialsValue = Contact.Name;
            }
            else
            {
                initialsValue = Contact?.Address;
            }

            return string.Join("", initialsValue
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]))); ;
        }

        public static ContactViewModel CreateEmpty(string address)
        {
            return new ContactViewModel(new AccountContact { Address = address }, null);
        }

        public static ContactViewModel CreateSelf(AccountProfile activeProfile)
        {
            return new ContactViewModel(
                new AccountContact { Address = activeProfile.Address },
                new ProfileData(activeProfile.ToString()),
                isSelf: true);
        }

        [RelayCommand]
        public void RaiseDisplayContact() => WeakReferenceMessenger.Default.Send(new ProfileDisplayRequested(Contact));
    }
}
