using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.ViewModels
{
    public partial class ProfileEditPageViewModel : BaseViewModel, IRecipient<ProfileImageUpdated>
    {


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGeneralSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsPersonalSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsWorkSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsInteretesSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsContactsSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(IsConfigurationSettingsVisible))]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        private int _selectedMenuIndex;

        [ObservableProperty]
        private ProfileData _userProfileData;

        #region General

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAwayMessageAvailable))]
        private bool _isAway;

        [ObservableProperty]
        private string _status;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AboutMessageMaxLength))]
        private string _about;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _awayMessage;

        public bool IsAwayMessageAvailable => IsAway;
        public int AboutMessageMaxLength => About?.Length ?? 0;

        #endregion

        #region Personal

        [ObservableProperty]
        private string _gender;

        [ObservableProperty]
        private string _languages;

        [ObservableProperty]
        private string _education;

        [ObservableProperty]
        private string _placesLived;

        [ObservableProperty]
        private string _relationshipStatus;

        [ObservableProperty]
        private string _birthDay;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NotesMessageLength))]
        private string _notes;

        public int NotesMessageLength => Notes?.Length ?? 0;

        #endregion

        #region Work

        [ObservableProperty]
        private string _work;

        [ObservableProperty]
        private string _jobTitle;

        [ObservableProperty]
        private string _organization;

        [ObservableProperty]
        private string _department;

        #endregion

        #region Interests

        [ObservableProperty]
        private string _interests;

        [ObservableProperty]
        private string _books;

        [ObservableProperty]
        private string _music;

        [ObservableProperty]
        private string _movies;

        [ObservableProperty]
        private string _sports;

        #endregion

        #region Contacts

        [ObservableProperty]
        private string _website;

        [ObservableProperty]
        private string _mailingAddress;

        [ObservableProperty]
        private string _location;

        [ObservableProperty]
        private string _phone;

        [ObservableProperty]
        private string _streams;


        #endregion

        #region Configuration

        [ObservableProperty]
        private bool _publicAccess;

        [ObservableProperty]
        private bool _publicLastSeen;

        [ObservableProperty]
        private bool _publicLinkQuerying;

        [ObservableProperty]
        private string _addressExpansion;

        #endregion

        public string ContactProfilePicture => $"{_applicationStateService.ActiveProfile.UserAddress.FullAddress}.png";

        public bool IsGeneralSettingsVisible => SelectedMenuIndex == 0;
        public bool IsPersonalSettingsVisible => SelectedMenuIndex == 1;
        public bool IsWorkSettingsVisible => SelectedMenuIndex == 2;
        public bool IsInteretesSettingsVisible => SelectedMenuIndex == 3;
        public bool IsContactsSettingsVisible => SelectedMenuIndex == 4;
        public bool IsConfigurationSettingsVisible => SelectedMenuIndex == 5;

        public string FullAddress => _applicationStateService.ActiveProfile.Address;

        public string PageTitle
        {
            get
            {
                return SelectedMenuIndex switch
                {
                    0 => "General",
                    1 => "Personal",
                    2 => "Work",
                    3 => "Interests",
                    4 => "Contacts",
                    5 => "Configuration",
                    _ => "Settings"
                };
            }
        }

        private readonly IProfileDataService _profileDataService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IDialogService _dialogService;
        private readonly ILoginService _loginService;
        private readonly IWindowService _windowService;
        private readonly IProfileDataManager _profileDataManager;

        public ProfileEditPageViewModel(IProfileDataService profileDataService,
                                     IApplicationStateService applicationStateService,
                                     IDialogService dialogService,
                                     ILoginService loginService,
                                     IWindowService windowService,
                                     IProfileDataManager profileDataManager)
        {
            _profileDataService = profileDataService;
            _applicationStateService = applicationStateService;
            _dialogService = dialogService;
            _loginService = loginService;
            _windowService = windowService;
            _profileDataManager = profileDataManager;
        }

        // General
        partial void OnAboutChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(About), newValue);
        partial void OnStatusChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Status), newValue);
        partial void OnIsAwayChanged(bool oldValue, bool newValue) => UserProfileData?.UpdateAttribute("Away", newValue.ToString());
        partial void OnAwayMessageChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Away-Warning", newValue);

        // Personal
        partial void OnBirthDayChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(BirthDay), newValue);
        partial void OnEducationChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Education), newValue);
        partial void OnGenderChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Gender), newValue);
        partial void OnLanguagesChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Languages), newValue);
        partial void OnNotesChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Notes), newValue);
        partial void OnPlacesLivedChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Places-Lived", newValue);
        partial void OnRelationshipStatusChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Relationship-Status", newValue);

        // Work
        partial void OnDepartmentChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Department), newValue);
        partial void OnJobTitleChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Job-Title", newValue);
        partial void OnOrganizationChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Organization), newValue);
        partial void OnWorkChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Work), newValue);

        // Interests
        partial void OnInterestsChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Interests), newValue);
        partial void OnBooksChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Books), newValue);
        partial void OnMusicChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Music), newValue);
        partial void OnMoviesChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Movies), newValue);
        partial void OnSportsChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Sports), newValue);

        // Contacts
        partial void OnLocationChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Location), newValue);
        partial void OnMailingAddressChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Mailing-Address", newValue);
        partial void OnPhoneChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Phone), newValue);
        partial void OnStreamsChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Streams), newValue);
        partial void OnWebsiteChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute(nameof(Website), newValue);


        // Configuration
        partial void OnAddressExpansionChanged(string oldValue, string newValue) => UserProfileData?.UpdateAttribute("Address-Expansion", newValue);
        partial void OnPublicAccessChanged(bool oldValue, bool newValue) => UserProfileData?.UpdateAttribute("Public-Access", newValue.ToString());
        partial void OnPublicLastSeenChanged(bool oldValue, bool newValue) => UserProfileData?.UpdateAttribute("Last-Seen-Public", newValue.ToString());
        partial void OnPublicLinkQueryingChanged(bool oldValue, bool newValue) => UserProfileData?.UpdateAttribute(nameof(PublicLinkQuerying), newValue.ToString());

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            // Load logged in user profile.
            UserProfileData = await _profileDataService.GetProfileDataAsync(_applicationStateService.ActiveProfile.UserAddress);

            if (UserProfileData == null)
            {
                // TODO: Error.
            }

            Name = UserProfileData.Name;

            IsAway = UserProfileData.Away ?? false;
            Status = UserProfileData.GetData<string>(nameof(Status));
            About = UserProfileData.GetData<string>(nameof(About));
            AwayMessage = UserProfileData.GetData<string>("Away-Warning");

            Gender = UserProfileData.GetData<string>(nameof(Gender));
            Languages = UserProfileData.GetData<string>(nameof(Languages));
            Education = UserProfileData.GetData<string>(nameof(Education));
            PlacesLived = UserProfileData.GetData<string>("Places-Lived");
            RelationshipStatus = UserProfileData.GetData<string>("Relationship-Status");
            BirthDay = UserProfileData.GetData<string>(nameof(BirthDay));
            Notes = UserProfileData.GetData<string>(nameof(Notes));

            Work = UserProfileData.GetData<string>(nameof(Work));
            JobTitle = UserProfileData.GetData<string>("Job-Title");
            Organization = UserProfileData.GetData<string>(nameof(Organization));
            Department = UserProfileData.GetData<string>(nameof(Department));

            Interests = UserProfileData.GetData<string>(nameof(Interests));
            Books = UserProfileData.GetData<string>(nameof(Books));
            Music = UserProfileData.GetData<string>(nameof(Music));
            Movies = UserProfileData.GetData<string>(nameof(Movies));
            Sports = UserProfileData.GetData<string>(nameof(Sports));

            Website = UserProfileData.GetData<string>(nameof(Website));
            MailingAddress = UserProfileData.GetData<string>("Mailing-Address");
            Location = UserProfileData.GetData<string>(nameof(Location));
            Phone = UserProfileData.GetData<string>(nameof(Phone));
            Streams = UserProfileData.GetData<string>(nameof(Streams));

            PublicAccess = UserProfileData.GetData<bool>("Public-Access");
            PublicLastSeen = UserProfileData.GetData<bool>("Last-Seen-Public");
            PublicLinkQuerying = UserProfileData.GetData<bool>(nameof(PublicLinkQuerying));
            AddressExpansion = UserProfileData.GetData<string>("Address-Expansion");

            // Enable editing properties now.
            UserProfileData.IsReadOnly = false;
        }

        public async void Receive(ProfileImageUpdated message)
        {
            // Shell will update first. This will make sure the preview is updated after the file is released.
            await Task.Delay(500);

            Dispatcher.ExecuteOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(ContactProfilePicture));
            });
        }

        [RelayCommand]
        private async Task ChangeAvatarAsync()
        {
            var pickedFileBytes = await _dialogService.ShowProfilePictureEditorAsync();

            if (pickedFileBytes?.Length == 0) return;

            try
            {
                var finalImageData = _profileDataManager.GetValidAvatar(pickedFileBytes);

                await _profileDataService.UpdateProfileImageAsync(_applicationStateService.ActiveProfile.UserAddress, finalImageData);
                await _profileDataManager.SaveProfileImageAsync(finalImageData, _applicationStateService.ActiveProfile.UserAddress).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new ProfileImageUpdated());
            }
            catch (Exception ex)
            {
                _dialogService.ShowInfoBarMessage("Failed", $"Couldn't update profile picture.\n{ex.Message}", Domain.Models.Shell.InfoBarMessageSeverity.Error);
            }
        }

        [RelayCommand]
        private Task DeleteAvatarAsync()
            => _profileDataService.DeleteProfileImageAsync(_applicationStateService.ActiveProfile.UserAddress);

        [RelayCommand]
        private Task SaveProfileAsync()
            => _profileDataService.UpdateProfileDataAsync(_applicationStateService.ActiveProfile.UserAddress, UserProfileData);
    }
}
