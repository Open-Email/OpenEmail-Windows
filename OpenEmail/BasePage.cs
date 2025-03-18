using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.ViewModels;
using OpenEmail.WinRT;

namespace OpenEmail
{
    public class BasePage : Page, IRecipient<DisposeViewModels>
    {
        public void Receive(DisposeViewModels message) => OnDisposeRequested();

        public virtual void OnDisposeRequested()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }

    public abstract class BasePage<T> : BasePage where T : BaseViewModel
    {
        public T ViewModel { get; } = App.Current.Services.GetService<T>();

        protected BasePage()
        {
            ViewModel.Dispatcher = new WinUIDispatcher(DispatcherQueue);
        }

        public override void OnDisposeRequested()
        {
            base.OnDisposeRequested();

            ViewModel.Dispatcher = null;
        }

        ~BasePage()
        {
            Debug.WriteLine($"Disposed {GetType().Name}");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var mode = GetNavigationMode(e.NavigationMode);
            var parameter = e.Parameter;

            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.RegisterAll(this);

            ViewModel.OnNavigatedTo(mode, parameter);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            var mode = GetNavigationMode(e.NavigationMode);
            var parameter = e.Parameter;

            WeakReferenceMessenger.Default.UnregisterAll(this);

            ViewModel.OnNavigatedFrom(mode, parameter);

            GC.Collect();
        }

        private FrameNavigationMode GetNavigationMode(NavigationMode mode) => (FrameNavigationMode)mode;
    }
}
