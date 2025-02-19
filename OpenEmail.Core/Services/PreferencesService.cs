using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Contracts.Application;

namespace OpenEmail.Core.Services
{
    public class PreferencesService : ObservableObject, IPreferencesService
    {
        private readonly IConfigurationService _configurationService;

        public PreferencesService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        private void SetPropertyAndSave(string propertyName, object value)
        {
            _configurationService.Set(propertyName, value);

            OnPropertyChanged(propertyName);
            Debug.WriteLine($"PreferencesService -> {propertyName}:{value?.ToString()}");
        }

        public int DownloadAttachmentLimitInMegabytes
        {
            get => _configurationService.Get(nameof(DownloadAttachmentLimitInMegabytes), 5);
            set => SetPropertyAndSave(nameof(DownloadAttachmentLimitInMegabytes), value);
        }

        public int SyncIntervalInMinutes
        {
            get => _configurationService.Get(nameof(SyncIntervalInMinutes), 15);
            set => SetPropertyAndSave(nameof(SyncIntervalInMinutes), value);
        }

        public int EmptyTrashIntervalInDays
        {
            get => _configurationService.Get(nameof(EmptyTrashIntervalInDays), 0);
            set => SetPropertyAndSave(nameof(EmptyTrashIntervalInDays), value);
        }

        public List<string> TrustedDomains
        {
            get => JsonSerializer.Deserialize<List<string>>(_configurationService.Get(nameof(TrustedDomains), "[]"));
            set => SetPropertyAndSave(nameof(TrustedDomains), JsonSerializer.Serialize(value));
        }

        public double MessageListingPaneWidth
        {
            get => _configurationService.Get(nameof(MessageListingPaneWidth), 350.0d);
            set => SetPropertyAndSave(nameof(MessageListingPaneWidth), value);
        }

        public double ShellMenuPageOpenWidth
        {
            get => _configurationService.Get(nameof(ShellMenuPageOpenWidth), 200.0d);
            set => SetPropertyAndSave(nameof(ShellMenuPageOpenWidth), value);
        }
    }
}
