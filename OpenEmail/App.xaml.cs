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

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await InitializeServicesAsync();

            var accountService = Services.GetService<IAccountService>();
            var appStateService = Services.GetService<IApplicationStateService>();

            appStateService.ActiveProfile = await accountService.GetStartAccountProfileAsync();

            LoadLoginPage();

            // bool hasProfileData = appStateService.ActiveProfile != null;

            //if (hasProfileData)
            //{
            //    LoadShell();
            //}
            //else
            //{
            //    LoadLoginPage();
            //}
        }

        public void LoadShell()
        {
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
            var loginWindow = WindowHelper.CreateWindow();
            SetupLoginWindow(loginWindow);

            loginWindow.Activate();
        }

        private void CreateWindow(bool isProfileLoaded)
        {
            //if (MainWindow != null)
            //{
            //    MainWindow.AppWindow.Closing -= AppWindowClosing;
            //    MainWindow.Close();
            //    MainWindow = null;
            //}

            //MainWindow = WindowHelper.CreateWindow();
            //MainWindow.AppWindow.Closing += AppWindowClosing;

            //MainWindow.Title = "Open Email";

            //WindowingFunctions.SetWindowIcon("Assets/appicon.ico", MainWindow);

            //if (isProfileLoaded)
            //{
            //    SetupMainWindow(MainWindow);
            //    (MainWindow.Content as Frame).Navigate(typeof(ShellPage));
            //}
            //else
            //{
            //    SetupLoginWindow(MainWindow);
            //    (MainWindow.Content as Frame).Navigate(typeof(LoginPage));
            //}

            //MainWindow.Activate();

            //var otherWindows = WindowHelper.ActiveWindows.Where(a => a != MainWindow).ToList();

            //foreach (var item in otherWindows)
            //{
            //    item.Close();
            //}

        }

        private void AppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;

            sender.Hide();
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
