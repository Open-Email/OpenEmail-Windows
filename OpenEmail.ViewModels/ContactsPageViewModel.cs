using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels
{
    public partial class ContactsPageViewModel : BaseViewModel,
        IRecipient<ProfileDataUpdated>,
        IRecipient<ContactCreated>,
        IRecipient<ContactUpdated>,
        IRecipient<ContactDeleted>,
        IRecipient<ProfileDisplayPaneDismissedMessage>
    {
        private readonly IContactService _contactService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IDialogService _dialogService;
        private readonly IPublicClientService _publicClientService;
        private readonly IProfileDataManager _profileDataManager;
        private readonly ILinksService _linksService;
        private readonly IProfileDataService _profileDataService;

        [ObservableProperty]
        private bool _isLoading;

        public readonly ObservableGroupedCollection<bool, ContactViewModel> ContactsSource = new ObservableGroupedCollection<bool, ContactViewModel>();

        public bool HasNoContacts => ContactsSource?.Count == 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedContact))]
        [NotifyPropertyChangedFor(nameof(IsNotInAddressBook))]
        [NotifyPropertyChangedFor(nameof(IsInAddressBook))]
        private ContactViewModel _selectedContactViewModel;

        public bool HasSelectedContact => SelectedContactViewModel != null;
        public bool IsNotInAddressBook => SelectedContactViewModel?.Contact.IsRequestAcccepted == false;
        public bool IsInAddressBook => SelectedContactViewModel?.Contact.IsRequestAcccepted == true;

        public ContactsPageViewModel(IContactService contactService,
                                     IApplicationStateService applicationStateService,
                                     IDialogService dialogService,
                                     IPublicClientService publicClientService,
                                     IProfileDataManager profileDataManager,
                                     ILinksService linksService,
                                     IProfileDataService profileDataService)
        {
            _contactService = contactService;
            _applicationStateService = applicationStateService;
            _dialogService = dialogService;
            _publicClientService = publicClientService;
            _profileDataManager = profileDataManager;
            _linksService = linksService;
            _profileDataService = profileDataService;
        }

        [RelayCommand]
        private async Task AcceptRequestAsync()
        {
            if (SelectedContactViewModel == null) return;

            // Automatically enable broadcasts.
            SelectedContactViewModel.Contact.ReceiveBroadcasts = true;

            bool isStored = await _linksService.StoreLinkAsync(_applicationStateService.ActiveProfile, SelectedContactViewModel.Contact);

            if (isStored)
            {
                await _contactService.AcceptContactRequestAsync(SelectedContactViewModel.Contact.UniqueId);
            }
        }

        [RelayCommand]
        private async Task DeleteContactAsync()
        {
            if (SelectedContactViewModel == null) return;

            // This is only a request.
            // There is no need to remove the link from the server.
            if (!SelectedContactViewModel.Contact.IsRequestAcccepted)
            {
                await _contactService.DeleteContactAsync(SelectedContactViewModel.Contact);
            }
            else
            {
                // Delete the link from the server.

                var sourceAddress = _applicationStateService.ActiveProfile.UserAddress;
                var targetAddress = UserAddress.CreateFromAddress(SelectedContactViewModel.Contact.Address);

                bool isRemoved = await _linksService.RemoveLinkAsync(sourceAddress, targetAddress);

                if (isRemoved)
                {
                    await _contactService.DeleteContactAsync(SelectedContactViewModel.Contact);

                    // Dismiss popup as well.
                    SelectedContactViewModel = null;
                }
            }
        }


        [RelayCommand]
        private async Task CreateMessageAsync()
        {

        }

        [RelayCommand]
        private async Task FetchMessagesAsync()
        {
            if (SelectedContactViewModel == null) return;
        }


        [RelayCommand]
        private Task RefreshContactAsync()
        {
            if (SelectedContactViewModel == null) return Task.CompletedTask;

            return _profileDataService.RefreshProfileDataAsync(UserAddress.CreateFromAddress(SelectedContactViewModel.Contact.Address));
        }

        [RelayCommand]
        private Task AddContactAsync() => _dialogService.ShowAddNewContactDialogAsync();


        [RelayCommand]
        private async Task BroadcastStateChangedAsync(bool isBroadcastOn)
        {
            if (SelectedContactViewModel == null) return;

            SelectedContactViewModel.Contact.ReceiveBroadcasts = isBroadcastOn;

            await _contactService.UpdateContactAsync(SelectedContactViewModel.Contact);
        }


        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            // TODO: This doesn't work all the time somehow...
            ContactsSource.CollectionChanged += ContactsUpdated;

            await ReloadRequestsAsync();
        }

        private void ContactsUpdated(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasNoContacts));
        }

        partial void OnSelectedContactViewModelChanged(ContactViewModel value)
        {
            if (value == null) return;

            if (value == null)
            {
                var message = new ProfileDisplayPaneDismissedMessage();
                Messenger.Send(message);
            }
            else
            {
                var message = new ProfileDisplayRequested(value.Contact);
                Messenger.Send(message);
            }
        }

        private async Task ReloadRequestsAsync(CancellationToken cancellationToken = default)
        {
            IsLoading = true;

            ContactsSource.Clear();

            var allContacts = await _contactService.GetContactsAsync(_applicationStateService.ActiveProfile.Account.Id);

            // Profile data for all images are saved locally while the contact is being created.
            // Fetch local cached data for all contacts and add them to the list.

            foreach (var contact in allContacts)
            {
                var localProfileData = await _profileDataManager.GetProfileDataAsync(UserAddress.CreateFromAddress(contact.Address), cancellationToken);

                // The data here is grouped by whether the request is accepted or not.
                ContactsSource.AddItem(contact.IsRequestAcccepted, new ContactViewModel(contact, localProfileData));
            }

            IsLoading = false;
        }

        public void Receive(ProfileDataUpdated message)
        {
            // Make sure to update profile data is the contact is in the list.
            var contactViewModelTuple = GetContactViewModelTuple(message.UserAddress.FullAddress);

            if (contactViewModelTuple == null) return;

            var group = contactViewModelTuple.Item1;
            var contact = contactViewModelTuple.Item2;

            ExecuteUIThread(() =>
            {
                contact.Profile = message.ProfileData;
            });
        }

        private Tuple<ObservableGroup<bool, ContactViewModel>, ContactViewModel> GetContactViewModelTuple(string contactAddress)
        {
            foreach (var group in ContactsSource)
            {
                foreach (var contact in group)
                {
                    if (contact.Contact.Address == contactAddress)
                    {
                        return new Tuple<ObservableGroup<bool, ContactViewModel>, ContactViewModel>(group, contact);
                    }
                }
            }

            return null;
        }

        public void Receive(ContactCreated message)
        {
            if (message.Contact.AccountId == _applicationStateService.ActiveProfile.Account.Id)
            {
                // Add new contact to the list.
                // TODO: Add to the list in the correct order or refresh the list.

                Dispatcher.ExecuteOnDispatcher(() =>
                {
                    ContactsSource.AddItem(message.Contact.IsRequestAcccepted, new ContactViewModel(message.Contact, message.ContactProfileData));
                });
            }
        }

        public void Receive(ContactUpdated message)
        {
            if (message.Contact.AccountId == _applicationStateService.ActiveProfile.Account.Id)
            {
                // Update the contact in the list.
                var contactViewModel = GetContactViewModelTuple(message.Contact.Address);

                if (contactViewModel == null) return;

                bool isGroupingChanged = contactViewModel.Item1.Key != message.Contact.IsRequestAcccepted;

                // Update contact + profile data first.
                Dispatcher.ExecuteOnDispatcher(() =>
                {
                    if (message.Contact != null) contactViewModel.Item2.Contact = message.Contact;
                    if (message.ContactProfileData != null) contactViewModel.Item2.Profile = message.ContactProfileData;
                });

                // When contact goes from request -> address book, grouping of the list changes.
                // In that case we move the item to proper group.
                if (isGroupingChanged)
                {
                    Dispatcher.ExecuteOnDispatcher(() =>
                    {
                        var newGroup = ContactsSource.FirstGroupByKeyOrDefault(message.Contact.IsRequestAcccepted);
                        var oldGroup = contactViewModel.Item1;

                        oldGroup.Remove(contactViewModel.Item2);

                        if (newGroup != null)
                        {
                            newGroup.Add(contactViewModel.Item2);
                        }
                        else
                        {
                            ContactsSource.AddItem(message.Contact.IsRequestAcccepted, contactViewModel.Item2);
                        }

                        SelectedContactViewModel = contactViewModel.Item2;
                    });
                }
            }
        }

        public void Receive(ContactDeleted message)
        {
            if (message.Contact.AccountId == _applicationStateService.ActiveProfile.Account.Id)
            {
                // Remove the contact from the list.
                var contactViewModel = GetContactViewModelTuple(message.Contact.Address);
                if (contactViewModel == null) return;

                Dispatcher.ExecuteOnDispatcher(() =>
                {
                    contactViewModel.Item1.Remove(contactViewModel.Item2);
                });
            }
        }

        public void Receive(ProfileDisplayPaneDismissedMessage message)
        {
            SelectedContactViewModel = null;
        }
    }
}
