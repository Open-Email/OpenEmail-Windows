using Microsoft.Extensions.DependencyInjection;
using OpenEmail.Contracts.Configuration;

namespace OpenEmail.WinRT
{
    public static class ContainerSetup
    {
        public static void RegisterWinRTServices(this IServiceCollection services)
        {
            services.AddSingleton<IApplicationConfiguration, ApplicationConfiguration>();
        }
    }
}
