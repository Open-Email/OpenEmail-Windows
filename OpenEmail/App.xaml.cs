using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Core;
using OpenEmail.Helpers;
using OpenEmail.ViewModels;
using OpenEmail.Views;
using OpenEmail.WinRT;
using SQLite;

namespace OpenEmail
{
    public partial class App : Application
    {

        public IServiceProvider Services { get; }
        public new static App Current => (App)Application.Current;

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.RegisterCoreServices();
            services.RegisterWinRTServices();
            services.RegisterApplicationServices();
            services.RegisterViewModels();

            return services.BuildServiceProvider();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeServicesAsync();
            await StartApplicationAsync();
        }

        public async Task StartApplicationAsync()
        {
            await InitializeServicesAsync();

            var accountService = Services.GetService<IAccountService>();
            var appStateService = Services.GetService<IApplicationStateService>();

            appStateService.ActiveProfile = await accountService.GetStartAccountProfileAsync();

            CreateWindow(appStateService.ActiveProfile != null);
        }

        private Task InitializeServicesAsync()
        {
            var databaseService = Services.GetRequiredService<IDatabaseService<ISQLiteAsyncConnection>>();
            return databaseService.InitializeAsync();
        }

        private void SetupLoginWindow(Window window)
        {
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(700, 900));

            WindowingFunctions.DisableMinimizeMaximizeButtons(window);
            WindowingFunctions.CenterWindowOnScreen(window);
        }

        private void CreateWindow(bool isProfileLoaded)
        {
            var newWindow = WindowHelper.CreateWindow();

            newWindow.Title = "Open Email";

            WindowingFunctions.SetWindowIcon("Assets/appicon.ico", newWindow);

            if (isProfileLoaded)
            {
                SetupMainWindow(newWindow);
                (newWindow.Content as Frame).Navigate(typeof(ShellPage));
            }
            else
            {
                SetupLoginWindow(newWindow);
                (newWindow.Content as Frame).Navigate(typeof(LoginPage));
            }

            newWindow.Activate();

            var otherWindows = WindowHelper.ActiveWindows.Where(a => a != newWindow).ToList();

            foreach (var item in otherWindows)
            {
                item.Close();
            }
        }

        private void SetupMainWindow(Window window)
        {
            // Configure title bar.
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
    }
}
