using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.ViewModels;
using OpenEmail.Views;
using WinUIEx;

namespace OpenEmail
{
    public sealed partial class ComposeWindow : WindowEx
    {
        private Frame _frame = new();
        private ComposerPageViewModel ViewModel => (_frame.Content as ComposerPage)?.ViewModel;

        public ComposeWindow(DraftComposeArgs args)
        {
            Title = "Open Email";

            MinWidth = 600;
            MinHeight = 600;

            Width = 800;
            Height = 600;

            IsTitleBarVisible = false;
            WindowingFunctions.CenterWindowOnScreen(this);

            Content = _frame;
            _frame.Navigate(typeof(ComposerPage), args);

            WindowingFunctions.BringToFront(this);

            Closed += WindowClosed;

            ViewModel.DismissWindow += DismissWindowRequested;

            if (AppWindow?.Presenter is not OverlappedPresenter presenter) return;

            presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
            ExtendsContentIntoTitleBar = true;

            if (_frame.Content is ComposerPage composerPage)
            {
                var titleBar = composerPage.GetTitleBar();
                SetTitleBar(titleBar);
            }
        }

        private void DismissWindowRequested(object sender, System.EventArgs e) => Close();

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            Closed -= WindowClosed;

            if (_frame == null) return;

            if (_frame.Content is ComposerPage composerPage)
            {
                // Make sure to dispose eveerything.
                composerPage.DisposePage();
            }
        }
    }
}
