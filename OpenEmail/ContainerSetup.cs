using Microsoft.Extensions.DependencyInjection;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.Services;
using OpenEmail.Services;
using OpenEmail.ViewModels.Interfaces;

namespace OpenEmail.WinRT
{
    public static class ContainerSetup
    {
        public static void RegisterApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IKeyStoreService, CredentialLockerKeyStoreProvider>();
            services.AddSingleton<IApplicationStateService, ApplicationStateService>();

            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IMessagePreperationService, MessagePreperationService>();
            services.AddTransient<IWindowService, WindowService>();
            services.AddTransient<IConfigurationService, ConfigurationService>();
        }
    }
}
