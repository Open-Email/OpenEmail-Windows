using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
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
using WinUIEx;

namespace OpenEmail
{
    public partial class App : Application
    {
        private bool isGoingBackLogin = false;

        public IServiceProvider Services { get; }
        public new static App Current => (App)Application.Current;

        // public static WindowEx MainWindow { get; set; }
        public static bool HandleClosedEvents { get; set; } = true;

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

        private async Task<bool> TryLoadProfileAsync()
        {
            var accountService = Services.GetService<IAccountService>();
            var appStateService = Services.GetService<IApplicationStateService>();

            appStateService.ActiveProfile = await accountService.GetStartAccountProfileAsync();

            return appStateService.ActiveProfile != null;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeServicesAsync();

            bool hasProfileData = await TryLoadProfileAsync();

            if (hasProfileData)
            {
                LoadShell();
            }
            else
            {
                LoadLoginPage();
            }
        }

        public async void LoadShell()
        {
            await TryLoadProfileAsync();

            isGoingBackLogin = false;

            var shellWindow = WindowHelper.CreateWindow();
            SetupMainWindow(shellWindow);

            shellWindow.Activate();
        }

        private Task InitializeServicesAsync()
        {
            var databaseService = Services.GetRequiredService<IDatabaseService<ISQLiteAsyncConnection>>();
            return databaseService.InitializeAsync();
        }

        private void SetupMainWindow(Window window)
        {
            window.Title = "Open Email";
            WindowingFunctions.SetWindowIcon("Assets/appicon.ico", window);

            var manager = WindowManager.Get(window);
            manager.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            manager.MinWidth = 800;
            manager.MinHeight = 600;

            (window.Content as Frame).Navigate(typeof(ShellPage));
        }

        private void SetupLoginWindow(Window window)
        {
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(700, 900));

            WindowingFunctions.DisableMinimizeMaximizeButtons(window);
            WindowingFunctions.CenterWindowOnScreen(window);
            WindowingFunctions.SetWindowIcon("Assets/appicon.ico", window);

            (window.Content as Frame).Navigate(typeof(LoginPage));
        }

        public void LoadLoginPage()
        {
            isGoingBackLogin = true;

            var loginWindow = WindowHelper.CreateWindow();
            SetupLoginWindow(loginWindow);

            loginWindow.Activate();
        }

        private void AppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!isGoingBackLogin)
            {
                args.Cancel = true;

                sender.Hide();
            }
        }

        public void TerminateApplication()
        {
            HandleClosedEvents = false;

            foreach (var window in WindowHelper.ActiveWindows)
            {
                window.Close();
            }

            Current.Exit();
        }
    }
}
