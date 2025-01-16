using Microsoft.Extensions.DependencyInjection;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.DataServices;
using OpenEmail.Core.Services;
using SQLite;

namespace OpenEmail.Core
{
    public static class ContainerSetup
    {
        public static void RegisterCoreServices(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService<ISQLiteAsyncConnection>, DatabaseService>();
            services.AddSingleton<IClientFactory, ClientFactory>();
            services.AddSingleton<IDiscoveryService, DiscoveryService>();
            services.AddSingleton<IPreferencesService, PreferencesService>();
            services.AddSingleton<IMessageUploader, MessageUploader>();

            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IPublicClientService, PublicClientService>();
            services.AddTransient<IMessagesService, MessagesService>();
            services.AddTransient<ILinksService, LinksService>();
            services.AddTransient<INotificationsService, NotificationsService>();
            services.AddTransient<IContactService, ContactService>();
            services.AddTransient<IProfileDataManager, ProfileDataManager>();
            services.AddTransient<IProfileDataService, ProfileDataService>();
            services.AddTransient<ILoginService, LoginService>();
            services.AddTransient<IQrService, QrService>();

            services.AddSingleton<IAttachmentManager, AttachmentManager>();
            services.AddSingleton<ISynchronizationService, SynchronizationService>();
        }
    }
}
