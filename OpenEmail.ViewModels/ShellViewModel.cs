using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.ViewModels
{
    public partial class ShellViewModel : BaseViewModel,
        IRecipient<StartAttachmentDownload>,
        IRecipient<ProfileDisplayRequested>,
        IRecipient<ProfileImageUpdated>,
        IRecipient<SendMessage>,
        IRecipient<TriggerSynchronizationMessage>
    {
        public event EventHandler NotifyIconDoubleClicked;

        public AccountProfile CurrentProfile => ApplicationStateService.ActiveProfile;

        [ObservableProperty]
        private int _selectedMenuItemIndex = -1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingContactRequests))]
        private int _pendingContactRequestCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsProfileLoadMenuVisible))]
        private ContactViewModel _displayingContactInformation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsProfileLoadMenuVisible))]
        private bool _isLoadingContactInformation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsProfileLoadMenuVisible))]
        private bool _isProfileLoadFailed;

        public bool IsProfileLoadMenuVisible => IsProfileLoadFailed || DisplayingContactInformation != null || IsLoadingContactInformation;

        public bool HasPendingContactRequests => PendingContactRequestCount > 0;
        public bool HasOngoingUploads => MessageUploader.MessageUploadQueue.Count > 0;

        public string HeaderText => CurrentProfile?.Address ?? string.Empty;
        public string TitleText => CurrentProfile?.DisplayName ?? string.Empty;
        public string ProfileImageUrl => $"{ApplicationStateService.ActiveProfile?.Address}.png";

        [ObservableProperty]
        public string synchronizationButtonText;

        public IApplicationStateService ApplicationStateService { get; }
        public IPreferencesService PreferencesService { get; }
        public IMessageUploader MessageUploader { get; }

        private CancellationTokenSource _contactInfoLoadCancellationTokenSource;
        private int remainingSyncIntervalMinutes;
        private DateTime? latestSynchronizationTime;

        private readonly System.Timers.Timer _syncIntervalTimer;
        private readonly IAttachmentManager _attachmentManager;
        private readonly ILoginService _loginService;
        private readonly IFileService _fileService;
        private readonly IContactService _contactService;
        private readonly IConfigurationService _configurationService;
        private readonly IMessagesService _messagesService;
        private readonly IWindowService _windowService;
        private readonly IDialogService _dialogService;
        private readonly IProfileDataService _profileDataService;
        private readonly ISynchronizationService _synchronizationService;

        public ShellViewModel(IApplicationStateService applicationStateService,
                              IAttachmentManager attachmentManager,
                              ILoginService loginService,
                              IFileService fileService,
                              IContactService contactService,
                              IConfigurationService configurationService,
                              IMessagesService messagesService,
                              IWindowService windowService,
                              IDialogService dialogService,
                              IPreferencesService preferencesService,
                              IMessageUploader messageUploader,
                              IProfileDataService profileDataService,
                              ISynchronizationService synchronizationService)
        {
            ApplicationStateService = applicationStateService;
            PreferencesService = preferencesService;
            PreferencesService.PropertyChanged += PreferencesUpdated;

            _attachmentManager = attachmentManager;
            _loginService = loginService;
            _fileService = fileService;
            _contactService = contactService;
            _configurationService = configurationService;
            _messagesService = messagesService;
            _windowService = windowService;
            _dialogService = dialogService;
            _profileDataService = profileDataService;
            _synchronizationService = synchronizationService;

            remainingSyncIntervalMinutes = preferencesService.SyncIntervalInMinutes;

            MessageUploader = messageUploader;
            MessageUploader.MessageUploadQueue.CollectionChanged += UploadQueueChanged;

            _syncIntervalTimer = new System.Timers.Timer(1 * 60 * 1000); // Check every minute.
            _syncIntervalTimer.Elapsed += SyncIntervalTick;
            _syncIntervalTimer.Start();
        }

        [RelayCommand]
        private void DoubleClicked() => NotifyIconDoubleClicked?.Invoke(this, EventArgs.Empty);

        private void PreferencesUpdated(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // We must update the new sync interval as well.
            if (e.PropertyName == nameof(IPreferencesService.SyncIntervalInMinutes))
            {
                remainingSyncIntervalMinutes = PreferencesService.SyncIntervalInMinutes;

                if (!SynchronizeCommand.IsRunning)
                {
                    UpdateSynchronizationIntervalText();
                }
            }
        }

        protected override void OnDisposeRequested()
        {
            base.OnDisposeRequested();

            _syncIntervalTimer.Stop();
            _syncIntervalTimer.Dispose();
        }

        protected override void OnDispatcherAssigned()
        {
            base.OnDispatcherAssigned();

            UpdateSynchronizationIntervalText();
        }

        private void SyncIntervalTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update the remaining time for synchronization.

            remainingSyncIntervalMinutes--;

            if (remainingSyncIntervalMinutes <= 0)
            {
                ExecuteUIThread(() => { SynchronizeCommand.Execute(null); });
            }
            else
            {
                UpdateSynchronizationIntervalText();
            }
        }

        private void UpdateSynchronizationIntervalText()
        {
            ExecuteUIThread(() =>
            {
                SynchronizationButtonText = $"Sync in {remainingSyncIntervalMinutes} minutes";
            });
        }

        private void UploadQueueChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.ExecuteOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(HasOngoingUploads));
            });
        }

        [RelayCommand]
        private void NewMessage()
        {
            var args = new ComposeWindowArgs(MailActionType.New);
            Messenger.Send(new NewComposeRequested(args));
        }

        [RelayCommand]
        private void NewBroadcast()
        {
            var args = new ComposeWindowArgs(MailActionType.Broadcast);
            Messenger.Send(new NewComposeRequested(args));
        }

        [RelayCommand]
        private void CloseProfileInformation()
        {
            DisplayingContactInformation = null;
            IsProfileLoadFailed = false;
            IsLoadingContactInformation = false;

            Messenger.Send(new ProfileDisplayPaneDismissedMessage());
        }

        [RelayCommand]
        private async Task SynchronizeAsync()
        {
            try
            {
                _syncIntervalTimer.Stop();

                ExecuteUIThread(() =>
                {
                    SynchronizationButtonText = "Synchronizing...";
                });

                await _synchronizationService.SynchronizeAsync(CurrentProfile);

                // TODO: Remove this line. Listen for synchronization complete event.
                PendingContactRequestCount = await _contactService.GetPendingContactRequestCountAsync(CurrentProfile.Account.Id);
            }
            catch (Exception ex)
            {
                _dialogService.ShowInfoBarMessage("Error", $"Failed to synchronize.\n{ex.Message}", InfoBarMessageSeverity.Error);
            }
            finally
            {
                _configurationService.Set("IsFirstSynchronizationCompleted", true);

                latestSynchronizationTime = DateTime.Now;
                remainingSyncIntervalMinutes = PreferencesService.SyncIntervalInMinutes;
                UpdateSynchronizationIntervalText();

                _syncIntervalTimer.Start();
            }
        }



        [RelayCommand]
        private async Task BroadcastStateChangedAsync(bool isBroadcastOn)
        {
            if (DisplayingContactInformation == null) return;

            DisplayingContactInformation.Contact.ReceiveBroadcasts = isBroadcastOn;

            await _contactService.UpdateContactAsync(DisplayingContactInformation.Contact);
        }

        public override async void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            base.OnNavigatedTo(navigationMode, parameter);

            OnPropertyChanged(nameof(CurrentProfile));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(ProfileImageUrl));

            PendingContactRequestCount = await _contactService.GetPendingContactRequestCountAsync(CurrentProfile.Account.Id);

            // Automatically navigate to Inbox.
            SelectedMenuItemIndex = 1;

            bool isFirstSyncCompleted = _configurationService.Get<bool>("IsFirstSynchronizationCompleted");

            if (!isFirstSyncCompleted)
            {
                await SynchronizeCommand.ExecuteAsync(null);
            }
        }

        public async void Receive(StartAttachmentDownload message)
        {
            try
            {
                await _attachmentManager.StartDownloadAttachmentAsync(message.Info).ConfigureAwait(false);

                var filePath = _attachmentManager.CreateAttachmentFilePath(message.Info.EnvelopeId, message.Info.FileName);

                if (message.LaunchAfter)
                {
                    await _fileService.LaunchFileAsync(filePath);
                }
                else if (!string.IsNullOrEmpty(message.SaveAfterPath))
                {
                    File.Copy(filePath, Path.Combine(message.SaveAfterPath, message.Info.FileName), true);
                }

            }
            catch (Exception ex)
            {
                _dialogService.ShowInfoBarMessage("Error", $"Failed to download attachment.\n{ex.Message}", InfoBarMessageSeverity.Error);
                throw;
            }
        }

        public void Receive(ProfileDisplayRequested message) => _ = LoadContactInformationAsync(message.Contact);

        private async Task LoadContactInformationAsync(AccountContact contact)
        {
            if (contact == null) return;

            // Don't load the same profile.
            if (DisplayingContactInformation?.Contact?.Address == contact.Address) return;

            Dispatcher.ExecuteOnDispatcher(() => IsLoadingContactInformation = true);

            if (_contactInfoLoadCancellationTokenSource != null)
            {
                _contactInfoLoadCancellationTokenSource.Cancel();
                _contactInfoLoadCancellationTokenSource.Dispose();
            }

            _contactInfoLoadCancellationTokenSource = new CancellationTokenSource();

            try
            {
                var profileData = await _profileDataService.GetProfileDataAsync(UserAddress.CreateFromAddress(contact.Address), _contactInfoLoadCancellationTokenSource.Token);

                DisplayingContactInformation = new ContactViewModel(contact, profileData);
            }
            catch (OperationCanceledException)
            {
                // Canceled by new request
            }
            catch (Exception)
            {
                Dispatcher.ExecuteOnDispatcher(() => IsProfileLoadFailed = true);
            }
            finally
            {
                Dispatcher.ExecuteOnDispatcher(() => IsLoadingContactInformation = false);
            }
        }

        public void Receive(ProfileImageUpdated message)
        {
            Dispatcher.ExecuteOnDispatcher(() =>
            {
                OnPropertyChanged(nameof(ProfileImageUrl));
            });
        }

        public void Receive(SendMessage message)
            => MessageUploader.UploadMessageAsync(message.MessageId, message.ReaderUploadMap, Dispatcher).ConfigureAwait(false);

        public void Receive(TriggerSynchronizationMessage message)
            => _ = SynchronizeAsync();
    }
}
