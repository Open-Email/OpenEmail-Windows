using System;
using System.Linq;
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
        public IServiceProvider Services { get; }
        public new static App Current => (App)Application.Current;

        public static WindowEx MainWindow { get; set; }
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
            if (MainWindow != null)
            {
                MainWindow.AppWindow.Closing -= AppWindowClosing;
                MainWindow.Close();
                MainWindow = null;
            }

            MainWindow = WindowHelper.CreateWindow();
            MainWindow.AppWindow.Closing += AppWindowClosing;

            MainWindow.Title = "Open Email";

            WindowingFunctions.SetWindowIcon("Assets/appicon.ico", MainWindow);

            if (isProfileLoaded)
            {
                SetupMainWindow(MainWindow);
                (MainWindow.Content as Frame).Navigate(typeof(ShellPage));
            }
            else
            {
                SetupLoginWindow(MainWindow);
                (MainWindow.Content as Frame).Navigate(typeof(LoginPage));
            }

            MainWindow.Activate();

            var otherWindows = WindowHelper.ActiveWindows.Where(a => a != MainWindow).ToList();

            foreach (var item in otherWindows)
            {
                item.Close();
            }

        }

        private void AppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;

            sender.Hide();
        }

        private void SetupMainWindow(Window window)
        {
            // Configure title bar.
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            if (window is WindowEx windowEx)
            {
                windowEx.MinWidth = 800;
                windowEx.MinHeight = 600;
            }
        }

        public void TerminateApplication()
        {
            HandleClosedEvents = false;

            foreach (var window in WindowHelper.ActiveWindows)
            {
                if (window == MainWindow) continue;

                window.Close();
            }

            Current.Exit();
        }
    }
}
