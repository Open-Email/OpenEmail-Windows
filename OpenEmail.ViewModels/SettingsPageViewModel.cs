using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Settings;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly IApplicationStateService _applicationStateService;
        private readonly IDialogService _dialogService;
        private readonly ILoginService _loginService;
        private readonly IWindowService _windowService;
        private readonly IQrService _qrService;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGeneralSectionVisible))]
        [NotifyPropertyChangedFor(nameof(IsTrustedDomainsSectionVisible))]
        [NotifyPropertyChangedFor(nameof(IsKeysSectionVisible))]
        private int _selectedTabIndex;

        public bool IsGeneralSectionVisible => SelectedTabIndex == 0;
        public bool IsTrustedDomainsSectionVisible => SelectedTabIndex == 1;
        public bool IsKeysSectionVisible => SelectedTabIndex == 2;


        #region General

        [ObservableProperty]
        private TimeInterval _selectedSyncInterval;

        [ObservableProperty]
        private TimeInterval _selectedEmptyTrashInterval;

        [ObservableProperty]
        private List<TimeInterval> _syncIntervals = new List<TimeInterval>()
        {
            new TimeInterval("5 minutes", TimeSpan.FromMinutes(5)),
            new TimeInterval("10 minutes", TimeSpan.FromMinutes(10)),
            new TimeInterval("15 minutes", TimeSpan.FromMinutes(15)),
            new TimeInterval("30 minutes", TimeSpan.FromMinutes(30)),
            new TimeInterval("1 hour", TimeSpan.FromHours(1)),
        };

        [ObservableProperty]
        private List<TimeInterval> _emptyTrashInterval = new List<TimeInterval>()
        {
            new TimeInterval("Never", TimeSpan.Zero),
            new TimeInterval("1 day", TimeSpan.FromDays(1)),
            new TimeInterval("3 days", TimeSpan.FromDays(3)),
            new TimeInterval("1 week", TimeSpan.FromDays(7)),
            new TimeInterval("1 month", TimeSpan.FromDays(30)),
        };

        #endregion

        #region Trusted Domains

        [ObservableProperty]
        private string _addDomainQuery;

        [ObservableProperty]
        public ObservableCollection<string> trustedDomains = new();

        public bool HasNoTrustedDomain => TrustedDomains.Count == 0;

        #endregion

        #region Keys

        [ObservableProperty]
        private byte[] _qrImage;

        public string PrivateSigningKey => _applicationStateService.ActiveProfile.PrivateSigningKeyBase64;
        public string PrivateEncryptionKey => _applicationStateService.ActiveProfile.PrivateEncryptionKeyBase64;
        public string PublicSigningKey => _applicationStateService.ActiveProfile.Account.PublicSigningKey;
        public string PublicEncryptionKey => _applicationStateService.ActiveProfile.Account.PublicEncryptionKey;

        public IPreferencesService PreferencesService { get; }

        #endregion

        public SettingsPageViewModel(IApplicationStateService applicationStateService,
                                     IPreferencesService preferencesService,
                                     IDialogService dialogService,
                                     ILoginService loginService,
                                     IWindowService windowService,
                                     IQrService qrService)
        {
            _applicationStateService = applicationStateService;
            _qrService = qrService;

            PreferencesService = preferencesService;
            _dialogService = dialogService;
            _loginService = loginService;
            _windowService = windowService;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            // Confirm the logout request.

            var isConfirmed = await _dialogService.ShowConfirmationDialogAsync("Logout", "Are you sure? All local data will be removed. Did you backup your keys?");

            if (!isConfirmed) return;

            await _loginService.LogoutAsync(_applicationStateService.ActiveProfile);

            // TODO: Cancel existing synchronizations.

            WeakReferenceMessenger.Default.Send(new DisposeViewModels());

            await _windowService.GoBackLoginAsync();
        }

        public override void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            TrustedDomains.CollectionChanged += DomainCollectionChanged;

            SelectedSyncInterval = SyncIntervals.FirstOrDefault(i => i.Duration.TotalMinutes == PreferencesService.SyncIntervalInMinutes);
            SelectedEmptyTrashInterval = EmptyTrashInterval.FirstOrDefault(i => i.Duration.TotalDays == PreferencesService.EmptyTrashIntervalInDays);

            foreach (var item in PreferencesService.TrustedDomains)
            {
                TrustedDomains.Add(item);
            }

            QrImage = _qrService.GetQrImage(_applicationStateService.ActiveProfile.PrivateEncryptionKeyBase64, _applicationStateService.ActiveProfile.PrivateSigningKeyBase64);
        }

        partial void OnSelectedSyncIntervalChanged(TimeInterval oldValue, TimeInterval newValue)
        {
            if (oldValue == null) return;

            PreferencesService.SyncIntervalInMinutes = (int)newValue.Duration.TotalMinutes;
        }

        partial void OnSelectedEmptyTrashIntervalChanged(TimeInterval oldValue, TimeInterval newValue)
        {
            if (oldValue == null) return;

            PreferencesService.EmptyTrashIntervalInDays = (int)newValue.Duration.TotalDays;
        }

        public override void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedFrom(navigationMode, parameter);

            TrustedDomains.CollectionChanged -= DomainCollectionChanged;
        }

        private void DomainCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasNoTrustedDomain));

            PreferencesService.TrustedDomains = TrustedDomains.ToList();
        }

        [RelayCommand]
        private async Task DeleteAccountAsync()
        {

        }

        [RelayCommand]
        private void AddTrustedDomain()
        {
            if (string.IsNullOrEmpty(AddDomainQuery) || TrustedDomains.Contains(AddDomainQuery)) return;

            TrustedDomains.Add(AddDomainQuery);

            AddDomainQuery = string.Empty;
        }

        [RelayCommand]
        private void DeleteTrustedDomain(string domain)
        {
            TrustedDomains.Remove(domain);
        }
    }
}
