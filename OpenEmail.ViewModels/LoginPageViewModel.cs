﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmailValidation;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Exceptions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Discovery;

namespace OpenEmail.ViewModels
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        private const int ENCRYPTION_LENGTH = 32;
        private const int SIGNING_LENGTH = 64;

        private readonly IAccountService _accountService;
        private readonly IDiscoveryService _discoveryService;
        private readonly ILoginService _loginService;
        private readonly IWindowService _windowService;
        private readonly IProfileDataService _profileDataService;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAuthenticate))]
        [NotifyCanExecuteChangedFor(nameof(AuthenticateCommand))]
        private string _privateEncryptionKey = "oKFDkqLP31CaiyZl+fkVx3MprymuTWZTIKU9/xkjcxs=";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAuthenticate))]
        [NotifyCanExecuteChangedFor(nameof(AuthenticateCommand))]
        private string _privateSigningKey = "RWXVx+wI7Z4WRpu6yKMyzkoET4mO5SggrSrTtVnSc/hJxmSbu9WPeDg3jHEF9uMMoalTlu7jxT0YNTG2rmZlmA==";

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
        private string _loggingInAddress = "burakwindows@open.email";

        public bool IsValidAddress => !string.IsNullOrWhiteSpace(LoggingInAddress) && EmailValidator.Validate(LoggingInAddress);

        public bool IsLoginSectionVisible => !IsCreateAccountSectionVisible && !IsAuthenticateSectionVisible;
        public bool IsAuthenticateSectionVisible => !IsCreateAccountSectionVisible && IsValidAddress && _isLoginInitiated;
        public bool IsCreateAccountSectionVisible => _isCreatingAccount;

        #region Account Creation

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        [NotifyPropertyChangedFor(nameof(IsInvalidFullName))]
        private string _fullName;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
        [NotifyPropertyChangedFor(nameof(IsInvalidLocalPart))]
        private string _localPart;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
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

        public LoginPageViewModel(IAccountService accountService,
                                  IDiscoveryService discoveryService,
                                  ILoginService loginService,
                                  IWindowService windowService,
                                  IProfileDataService profileDataService)
        {
            _accountService = accountService;
            _discoveryService = discoveryService;
            _loginService = loginService;
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

                await _windowService.RestartApplicationAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(IsValidAddress))]
        private void Login()
        {
            if (!IsValidAddress) return;

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
                    await _windowService.RestartApplicationAsync();
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
    }
}
