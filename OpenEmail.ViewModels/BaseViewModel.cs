using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Navigation;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.ViewModels
{
    public partial class BaseViewModel : ObservableRecipient, INavigationAware, IRecipient<DisposeViewModels>
    {
        [ObservableProperty]
        private bool _isLoading;

        private IPlatformDispatcher _dispatcher;
        public IPlatformDispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
            set
            {
                _dispatcher = value;

                if (value != null)
                {
                    OnDispatcherAssigned();
                }
            }
        }

        public void ExecuteUIThread(Action action) => Dispatcher?.ExecuteOnDispatcher(action);
        protected virtual void OnDispatcherAssigned() { }

        public virtual void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter)
        {
            Messenger.UnregisterAll(this);
        }

        public virtual void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter)
        {
            Messenger.RegisterAll(this);
        }

        public void Receive(DisposeViewModels message)
        {
            Messenger.UnregisterAll(this);
            Dispatcher = null;
        }
    }
}
