using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.Helpers;
using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class ShellPageAbstract : BasePage<ShellViewModel>
    {
        public bool IsDisplayingProfileControl
        {
            get { return (bool)GetValue(IsDisplayingProfileControlProperty); }
            set { SetValue(IsDisplayingProfileControlProperty, value); }
        }

        public static readonly DependencyProperty IsDisplayingProfileControlProperty = DependencyProperty.Register(nameof(IsDisplayingProfileControl), typeof(bool), typeof(ShellPageAbstract), new PropertyMetadata(false));
    }

    public sealed partial class ShellPage : ShellPageAbstract,
        IRecipient<NewComposeRequested>,
        IRecipient<DraftComposeArgs>,
        IRecipient<DisplayInfoMessage>
    {


        public ShellPage()
        {
            InitializeComponent();

            ViewModel.NotifyIconDoubleClicked += NotifyIconClicked;
        }

        private void NotifyIconClicked(object sender, EventArgs e) => LaunchClicked(null, null);

        private void PaneButtonClicked(TitleBar sender, object args)
        {
            MainNavigationView.IsPaneOpen = !MainNavigationView.IsPaneOpen;
        }

        public override void OnDisposeRequested()
        {
            base.OnDisposeRequested();

            ViewModel.NotifyIconDoubleClicked -= NotifyIconClicked;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        private void NavigatePageType(PageType pageType, object additionalArgs = null)
        {
            var currentPageType = GetFrameContentType();

            if (currentPageType == pageType) return;

            // If the selected page is mail related, we need to check if the current page is mail related.
            bool isMailListPageRequested = pageType is PageType.MailDrafts
                                            or PageType.MailInbox
                                            or PageType.MailOutbox
                                            or PageType.MailTrash;

            bool isMailListPageCurrent =
                currentPageType is PageType.MailDrafts or
                PageType.MailInbox or
                PageType.MailOutbox or
                PageType.MailTrash;

            if (isMailListPageRequested && isMailListPageCurrent)
            {
                if (additionalArgs is ComposeWindowArgs composeWindowArgs)
                {
                    WeakReferenceMessenger.Default.Send(composeWindowArgs);
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new ListingFolderChanged((MailFolder)pageType));
                }

                return;
            }

            var defaultTransition = new EntranceNavigationTransitionInfo();

            if (pageType == PageType.ContactsPage)
            {
                LeftFrame.Navigate(typeof(ContactsPage), null, defaultTransition);
            }
            else if (pageType == PageType.BroadcastPage)
            {
                LeftFrame.Navigate(typeof(BroadcastPage), null, defaultTransition);
            }
            else
            {
                // Mail list page.
                LeftFrame.Navigate(typeof(MailListPage), additionalArgs == null ? (MailFolder)pageType : additionalArgs, defaultTransition);
            }
        }

        private void NavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer == null) return;

            if (args.IsSettingsSelected)
            {
                LeftFrame.Navigate(typeof(SettingsPage), null, new EntranceNavigationTransitionInfo());
            }
            else if (args.SelectedItemContainer.Tag is int selectedPageTypeIndex)
            {
                var pageType = (PageType)selectedPageTypeIndex;
                NavigatePageType(pageType);
            }

            // This will close the contact profile pane.
            ViewModel.DisplayingContactInformation = null;
        }

        private PageType? GetFrameContentType()
        {
            if (LeftFrame.Content == null) return null;
            var contentType = LeftFrame.Content.GetType();

            return contentType switch
            {
                Type _ when contentType == typeof(SettingsPage) => PageType.SettingsPage,
                Type _ when contentType == typeof(ContactsPage) => PageType.ContactsPage,
                Type _ when contentType == typeof(BroadcastPage) => PageType.BroadcastPage,
                Type _ when contentType == typeof(ProfileEditPage) => PageType.ProfileEditPage,
                Type _ when contentType == typeof(MailListPage) && (LeftFrame.Content as MailListPage).ViewModel.CurrentMailFolderType == MailFolder.Drafts => PageType.MailDrafts,
                Type _ when contentType == typeof(MailListPage) && (LeftFrame.Content as MailListPage).ViewModel.CurrentMailFolderType == MailFolder.Inbox => PageType.MailInbox,
                Type _ when contentType == typeof(MailListPage) && (LeftFrame.Content as MailListPage).ViewModel.CurrentMailFolderType == MailFolder.Outbox => PageType.MailOutbox,
                Type _ when contentType == typeof(MailListPage) && (LeftFrame.Content as MailListPage).ViewModel.CurrentMailFolderType == MailFolder.Trash => PageType.MailTrash,
                _ => throw new NotImplementedException(),
            };
        }

        private void ProfileMenuClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ProfileFlyout.Hide();

            LeftFrame.Navigate(typeof(ProfileEditPage), null, new EntranceNavigationTransitionInfo());

            MainNavigationView.SelectedItem = null;
        }

        public void Receive(NewComposeRequested message)
        {
            if (GetFrameContentType() == PageType.MailDrafts)
            {
                // Send the args directly. We are already in drafts page.
                WeakReferenceMessenger.Default.Send(message.Args);
            }
            else
            {
                // Go to drafts folder
                NavigatePageType(PageType.MailDrafts, message.Args);

                ViewModel.SelectedMenuItemIndex = 3;
            }
        }

        public void Receive(DraftComposeArgs message)
        {
            var window = WindowHelper.CreateWindow(new ComposeWindow(message));

            window.Activate();
        }

        public void Receive(DisplayInfoMessage message)
        {
            switch (message.Severity)
            {
                case InfoBarMessageSeverity.Success:
                    InfoBarMessage.Severity = InfoBarSeverity.Success;
                    break;
                case InfoBarMessageSeverity.Info:
                    InfoBarMessage.Severity = InfoBarSeverity.Informational;
                    break;
                case InfoBarMessageSeverity.Warning:
                    InfoBarMessage.Severity = InfoBarSeverity.Warning;
                    break;
                case InfoBarMessageSeverity.Error:
                    InfoBarMessage.Severity = InfoBarSeverity.Error;
                    break;
                default:
                    break;
            }

            InfoBarMessage.Title = message.Title;
            InfoBarMessage.Message = message.Message;
            InfoBarMessage.IsOpen = true;
            InfoBarMessage.IsClosable = !message.AutoDismiss;

            if (message.AutoDismiss)
            {
                StartAutoDismissInfoBar();
            }
        }

        private void StartAutoDismissInfoBar()
        {
            Task.Delay(3000).ContinueWith(_ =>
            {
                ViewModel.Dispatcher.ExecuteOnDispatcher(() =>
                {
                    InfoBarMessage.IsOpen = false;
                });
            });
        }

        private void ExitClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            App.Current.TerminateApplication();
        }

        private void LaunchClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (App.MainWindow == null) return;

            App.MainWindow.Activate();
        }

        private void NavigationPaneButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            MainNavigationView.IsPaneOpen = !MainNavigationView.IsPaneOpen;
        }

        private void NewMailShortcutInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            ViewModel.NewMessageCommand.Execute(null);
        }
    }
}
