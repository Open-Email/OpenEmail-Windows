using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.ViewModels.Data;


namespace OpenEmail.Controls
{
    public sealed partial class ContactProfileControl : UserControl
    {
        public event EventHandler<bool> ProfileLoadCompleted;

        protected IProfileDataService ProfileDataService { get; } = App.Current.Services.GetService<IProfileDataService>();

        public ContactViewModel Contact
        {
            get { return (ContactViewModel)GetValue(ContactProperty); }
            set { SetValue(ContactProperty, value); }
        }

        public bool IsBroadcastOn
        {
            get { return (bool)GetValue(IsBroadcastOnProperty); }
            set { SetValue(IsBroadcastOnProperty, value); }
        }

        public ICommand BroadcastStateChangedCommand
        {
            get { return (ICommand)GetValue(BroadcastStateChangedCommandProperty); }
            set { SetValue(BroadcastStateChangedCommandProperty, value); }
        }

        public bool IsLoadingProfile
        {
            get { return (bool)GetValue(IsLoadingProfileProperty); }
            set { SetValue(IsLoadingProfileProperty, value); }
        }

        public double DesiredMaxImageSize
        {
            get { return (double)GetValue(DesiredMaxImageSizeProperty); }
            set { SetValue(DesiredMaxImageSizeProperty, value); }
        }

        public bool IsProfileLoadFailed
        {
            get { return (bool)GetValue(IsProfileLoadFailedProperty); }
            set { SetValue(IsProfileLoadFailedProperty, value); }
        }

        public static readonly DependencyProperty IsProfileLoadFailedProperty = DependencyProperty.Register(nameof(IsProfileLoadFailed), typeof(bool), typeof(ContactProfileControl), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLoadingProfileProperty = DependencyProperty.Register(nameof(IsLoadingProfile), typeof(bool), typeof(ContactProfileControl), new PropertyMetadata(false));
        public static readonly DependencyProperty IsBroadcastOnProperty = DependencyProperty.Register(nameof(IsBroadcastOn), typeof(bool), typeof(ContactProfileControl), new PropertyMetadata(false, new PropertyChangedCallback(OnBroadcastStateChanged)));
        public static readonly DependencyProperty ContactProperty = DependencyProperty.Register(nameof(Contact), typeof(ContactViewModel), typeof(ContactProfileControl), new PropertyMetadata(null, new PropertyChangedCallback(OnContactChanged)));
        public static readonly DependencyProperty BroadcastStateChangedCommandProperty = DependencyProperty.Register(nameof(BroadcastStateChangedCommand), typeof(ICommand), typeof(ContactProfileControl), new PropertyMetadata(null));
        public static readonly DependencyProperty IsClosePaneButtonVisibleProperty = DependencyProperty.Register(nameof(IsClosePaneButtonVisible), typeof(bool), typeof(ContactProfileControl), new PropertyMetadata(false));
        public static readonly DependencyProperty ClosePaneCommandProperty = DependencyProperty.Register(nameof(ClosePaneCommand), typeof(ICommand), typeof(ContactProfileControl), new PropertyMetadata(null));
        public static readonly DependencyProperty DesiredMaxImageSizeProperty = DependencyProperty.Register(nameof(DesiredMaxImageSize), typeof(double), typeof(ContactProfileControl), new PropertyMetadata(400d));

        public bool IsClosePaneButtonVisible
        {
            get { return (bool)GetValue(IsClosePaneButtonVisibleProperty); }
            set { SetValue(IsClosePaneButtonVisibleProperty, value); }
        }

        public ICommand ClosePaneCommand
        {
            get { return (ICommand)GetValue(ClosePaneCommandProperty); }
            set { SetValue(ClosePaneCommandProperty, value); }
        }

        public ContactProfileControl() => InitializeComponent();

        private static void OnBroadcastStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContactProfileControl control)
            {
                control.NotifyBroadcastStateChanged();
            }
        }

        private static void OnContactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContactProfileControl control)
            {
                _ = control.UpdateContactAsync();
            }
        }

        private void NotifyBroadcastStateChanged()
        {
            BroadcastStateChangedCommand?.Execute(IsBroadcastOn);
        }

        private async Task UpdateContactAsync()
        {
            if (Contact == null) return;

            IsProfileLoadFailed = false;
            IsBroadcastOn = Contact.Contact.ReceiveBroadcasts;

            // Refresh profile if doesn't exists.
            if (Contact.Profile == null)
            {
                try
                {
                    IsLoadingProfile = true;
                    Contact.Profile = await ProfileDataService.GetProfileDataAsync(UserAddress.CreateFromAddress(Contact.Contact.Address));

                    ProfileLoadCompleted?.Invoke(this, true);
                    IsLoadingProfile = false;
                }
                catch (Exception)
                {
                    IsLoadingProfile = false;
                    IsProfileLoadFailed = true;
                    ProfileLoadCompleted?.Invoke(this, false);
                }
            }
        }

        private async void RetryClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button senderButton)
            {
                senderButton.IsEnabled = false;

                await UpdateContactAsync();

                senderButton.IsEnabled = true;
            }
        }
    }
}
