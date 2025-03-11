using Microsoft.Extensions.DependencyInjection;
using OpenEmail.ViewModels.Dialogs;

namespace OpenEmail.ViewModels
{
    public static class ContainerSetup
    {
        public static void RegisterViewModels(this IServiceCollection services)
        {
            services.AddSingleton<ShellViewModel>();
            services.AddTransient<MailListPageViewModel>();
            services.AddTransient<ProfileEditPageViewModel>();
            services.AddTransient<ContactsPageViewModel>();
            services.AddTransient<BroadcastPageViewModel>();
            services.AddTransient<LoginPageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<ComposerPageViewModel>();

            // Dialogs
            services.AddTransient<AddNewContactDialogViewModel>();
            services.AddTransient<ContactProfileDisplayPopupDialogViewModel>();
        }
    }
}
