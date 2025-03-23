using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmailValidation;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Exceptions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Discovery;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.ViewModels
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        private const int ENCRYPTION_LENGTH = 32;
        private const int SIGNING_LENGTH = 64;

        private readonly IAccountService _accountService;
        private readonly IDiscoveryService _discoveryService;
        private readonly IPublicClientService _publicClientService;
        private readonly ILoginService _loginService;
        private readonly IDialogService _dialogService;
        private readonly IWindowService _windowService;
        private readonly IProfileDataService _profileDataService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAuthenticate))]
        [NotifyCanExecuteChangedFor(nameof(AuthenticateCommand))]
        private string _privateEncryptionKey;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAuthenticate))]
        [NotifyCanExecuteChangedFor(nameof(AuthenticateCommand))]
        private string _privateSigningKey;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProfileThumbnailUrl))]
        public partial ProfileData LoadedProfile { get; set; }

        public string ProfileThumbnailUrl => $"{LoggingInAddress}_thumbnail.png";

        public bool CanAuthenticate

        {
            get
            {
                // Check validity of base 64 encoded keys
                if (IsValidBase64(PrivateEncryptionKey) && IsValidBase64(PrivateSigningKey))
                {
                    return Convert.FromBase64String(PrivateEncryptionKey).Length == ENCRYPTION_LENGTH && Convert.FromBase64String(PrivateSigningKey).Length == SIGNING_LENGTH;
                }

                return false;
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValidAddress))]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _loggingInAddress;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNameValidationVisible))]
        private bool _isCheckingNameAvailability;

        [ObservableProperty]
        private bool _isNameAvailable;

        public bool IsNameValidationVisible => !IsCheckingNameAvailability && !string.IsNullOrEmpty(LocalPart);

        public bool IsValidAddress => !string.IsNullOrWhiteSpace(LoggingInAddress) && EmailValidator.Validate(LoggingInAddress);
        public bool IsLoginSectionVisible => !IsCreateAccountSectionVisible && !IsAuthenticateSectionVisible;
        public bool IsAuthenticateSectionVisible => !IsCreateAccountSectionVisible && IsValidAddress && _isLoginInitiated;
        public bool IsCreateAccountSectionVisible => _isCreatingAccount;

        #region Account Creation

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        [NotifyPropertyChangedFor(nameof(IsInvalidFullName))]
        private string _fullName;

        public string FullAddress => $"{LocalPart}@{SelectedHost?.HostPart ?? string.Empty}";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        [NotifyPropertyChangedFor(nameof(IsInvalidLocalPart))]
        [NotifyPropertyChangedFor(nameof(FullAddress))]
        private string _localPart;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        [NotifyPropertyChangedFor(nameof(FullAddress))]
        private DiscoveryHost _selectedHost;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage;

        public bool CanCreateAccount => !IsInvalidLocalPart && !IsInvalidFullName && !IsLoading;

        public bool IsInvalidFullName => string.IsNullOrWhiteSpace(FullName);
        public bool IsInvalidLocalPart => string.IsNullOrWhiteSpace(LocalPart);
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public List<DiscoveryHost> AvailableHosts { get; } =
        [
            new DiscoveryHost("mail.open.email", "open.email"),
        ];

        #endregion

        private bool _isLoginInitiated = false;
        private bool _isCreatingAccount = false;
        private CancellationTokenSource _nameExistenceCancellationTokenSource;

        public LoginPageViewModel(IAccountService accountService,
                                  IDiscoveryService discoveryService,
                                  IPublicClientService publicClientService,
                                  ILoginService loginService,
                                  IDialogService dialogService,
                                  IWindowService windowService,
                                  IProfileDataService profileDataService)
        {
            _accountService = accountService;
            _discoveryService = discoveryService;
            _publicClientService = publicClientService;
            _loginService = loginService;
            _dialogService = dialogService;
            _windowService = windowService;
            _profileDataService = profileDataService;

            // Automatically select the first host.
            SelectedHost = AvailableHosts.First();
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanAuthenticate))]
        private async Task AuthenticateAsync()
        {
            try
            {
                var authenticatedAccount = await _loginService.AuthenticateAsync(UserAddress.CreateFromAddress(LoggingInAddress), PrivateEncryptionKey, PrivateSigningKey);

                if (authenticatedAccount != null)
                {
                    _windowService.StartShellApplication();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(IsValidAddress))]
        private async Task LoginAsync()
        {
            if (!IsValidAddress) return;

            // Get profile data.
            try
            {
                LoadedProfile = null;
                LoadedProfile = await _profileDataService.RefreshProfileDataAsync(UserAddress.CreateFromAddress(LoggingInAddress));
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Error", ex.Message);
            }

            if (LoadedProfile == null)
            {
                return;
            }

            _isLoginInitiated = true;

            UpdateSectionVisibilities();
        }

        [RelayCommand]
        public void Back()
        {
            // Don't allow back navigation when authentication is in progress.
            if (AuthenticateCommand.IsRunning) return;

            _isCreatingAccount = false;
            _isLoginInitiated = false;

            LoggingInAddress = string.Empty;
            ErrorMessage = string.Empty;

            UpdateSectionVisibilities();
        }

        [RelayCommand]
        private void GoCreateAccount()
        {
            _isCreatingAccount = true;

            ErrorMessage = string.Empty;

            UpdateSectionVisibilities();
        }

        [RelayCommand(CanExecute = nameof(CanCreateAccount))]
        private async Task CreateAccountAsync()
        {
            IsLoading = true;

            try
            {
                // Resolve agents for given hosts first.
                var resolvedMailAgentHost = await _discoveryService.GetDiscoveryHostAsync(SelectedHost.HostPart)
                    ?? throw new AccountCreationFailedException("Failed to resolve mail agent host.");

                var createdAccount = await _accountService.CreateAccountAsync(LocalPart, FullName, resolvedMailAgentHost);

                if (createdAccount != null)
                {
                    // Account is created. Ready to load the account.
                    // Get the profile for the account and restart the app.

                    await _profileDataService.RefreshProfileDataAsync(createdAccount.Address);
                    _windowService.StartShellApplication();
                }
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }

            IsLoading = false;
        }

        private void UpdateSectionVisibilities()
        {
            OnPropertyChanged(nameof(IsLoginSectionVisible));
            OnPropertyChanged(nameof(IsCreateAccountSectionVisible));
            OnPropertyChanged(nameof(IsAuthenticateSectionVisible));
        }

        public static bool IsValidBase64(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch (FormatException)
            {
                // Catch format exceptions if input is not valid Base64
                return false;
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(LocalPart) && SelectedHost != null)
            {
                _nameExistenceCancellationTokenSource?.Cancel();
                _nameExistenceCancellationTokenSource = new CancellationTokenSource();

                _ = CheckNameAvailability(_nameExistenceCancellationTokenSource.Token);
            }
        }

        private async Task CheckNameAvailability(CancellationToken cancellationToken)
        {
            try
            {
                ExecuteUIThread(() => IsCheckingNameAvailability = true);

                var isAvailable = await _loginService.IsUsernameAvailableAsync("", new UserAddress(LocalPart, SelectedHost.HostPart), cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested) return;

                ExecuteUIThread(() => IsNameAvailable = isAvailable);
            }
            catch (Exception)
            {
                // Ignore errors.
            }
            finally
            {
                ExecuteUIThread(() => IsCheckingNameAvailability = false);
            }
        }
    }
}
