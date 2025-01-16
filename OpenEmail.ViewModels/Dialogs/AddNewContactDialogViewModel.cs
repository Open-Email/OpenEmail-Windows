using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmailValidation;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels.Dialogs
{
    public partial class AddNewContactDialogViewModel : BaseDialogViewModel
    {
        private readonly IContactService _contactService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IProfileDataService _profileDataService;
        private readonly ILinksService _linksService;
        private readonly IProfileDataManager _profileDataManager;
        private readonly IPublicClientService _publicClientService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAddContactButtonEnabled))]
        [NotifyCanExecuteChangedFor(nameof(AddContactCommand))]
        private string _contactAddress;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldDisplaySearch))]
        [NotifyPropertyChangedFor(nameof(IsAddContactButtonEnabled))]
        [NotifyPropertyChangedFor(nameof(CanChangeContactAddress))]
        private bool _isSearchingProfile;

        public bool IsContactFound => FoundContact != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsContactFound))]
        [NotifyPropertyChangedFor(nameof(PrimaryCommandText))]
        private ContactViewModel _foundContact;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isBroadcastOn;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PrimaryCommandText))]
        private bool isApprovingLocalContactRequest;

        public bool ShouldDisplaySearch => !IsContactFound;
        public bool IsValidAddress => !string.IsNullOrWhiteSpace(ContactAddress) && EmailValidator.Validate(ContactAddress);
        public bool IsAddContactButtonEnabled => IsValidAddress && !IsSearchingProfile;

        public bool CanChangeContactAddress => !IsSearchingProfile;
        public string PrimaryCommandText => !IsSearchingProfile ? "Add" : (IsApprovingLocalContactRequest ? "Approve" : "Save");
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public AddNewContactDialogViewModel(IContactService contactService,
                                            IApplicationStateService applicationStateService,
                                            IProfileDataService profileDataService,
                                            ILinksService linksService,
                                            IProfileDataManager profileDataManager,
                                            IPublicClientService publicClientService)
        {
            _contactService = contactService;
            _applicationStateService = applicationStateService;
            _profileDataService = profileDataService;
            _linksService = linksService;
            _profileDataManager = profileDataManager;
            _publicClientService = publicClientService;
        }

        [RelayCommand(CanExecute = nameof(IsAddContactButtonEnabled))]
        private async Task AddContactAsync()
        {
            if (!IsValidAddress) return;

            if (FoundContact != null)
            {
                await SaveContactAsync();
            }
            else
            {
                await PerformSearchContactAsync();
            }
        }

        private async Task SaveContactAsync()
        {
            // Save this link on the server.
            bool isLinkSaved = await _linksService.StoreLinkAsync(_applicationStateService.ActiveProfile,
                                                                  UserAddress.CreateFromAddress(FoundContact.Contact.Address));

            if (isLinkSaved)
            {
                FoundContact.Contact.ReceiveBroadcasts = IsBroadcastOn;
                FoundContact.Contact.IsRequestAcccepted = true;
                FoundContact.Contact.CreatedAt = DateTimeOffset.Now;

                await _contactService.AddOrUpdateContactAsync(_applicationStateService.ActiveProfile, FoundContact.Contact, FoundContact.Profile);

                Hide();
            }
        }

        private async Task PerformSearchContactAsync()
        {
            try
            {
                FoundContact = null;
                ErrorMessage = string.Empty;
                IsSearchingProfile = true;
                IsApprovingLocalContactRequest = false;

                // Check contact existence.

                var existingContact = await _contactService.GetContactAsync(_applicationStateService.ActiveProfile.Account.Id, ContactAddress);

                if (existingContact != null && existingContact.IsRequestAcccepted) throw new Exception("This contact is already in your address book.");

                var address = UserAddress.CreateFromAddress(ContactAddress);

                if (existingContact == null)
                {
                    // Download and save the profile data.

                    var profileData = await _profileDataService.RefreshProfileDataAsync(address);
                    var accountLink = AccountLink.Create(_applicationStateService.ActiveProfile.Account.Address, UserAddress.CreateFromAddress(ContactAddress));
                    var contact = new AccountContact()
                    {
                        UniqueId = Guid.NewGuid(),
                        AccountId = _applicationStateService.ActiveProfile.Account.Id,
                        Address = ContactAddress,
                        IsRequestAcccepted = false,
                        CreatedAt = DateTimeOffset.Now,
                        ReceiveBroadcasts = false,
                        Link = accountLink.Link,
                        Name = profileData.Name,
                        Id = "n/a"
                    };

                    FoundContact = new ContactViewModel(contact, profileData);
                }
                else
                {
                    // Contact exists as request, not approved.
                    // Load the profile data and show the contact.

                    IsApprovingLocalContactRequest = true;

                    var contact = await _contactService.GetContactAsync(_applicationStateService.ActiveProfile.Account.Id, ContactAddress);
                    var profileData = await _profileDataManager.GetProfileDataAsync(address);

                    FoundContact = new ContactViewModel(contact, profileData);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsSearchingProfile = false;
            }
        }

        [RelayCommand]
        private void CloseDialog() => Hide();

        [RelayCommand]
        private async Task BroadcastStateChangedAsync(bool isBroadcastOn)
        {
            if (FoundContact == null) return;

            FoundContact.Contact.ReceiveBroadcasts = isBroadcastOn;

            await _contactService.UpdateContactAsync(FoundContact.Contact);
        }
    }
}
