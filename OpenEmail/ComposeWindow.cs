using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.ViewModels;
using OpenEmail.Views;

namespace OpenEmail
{
    public class ComposeWindow : Window
    {
        private Frame _frame = new();
        private ComposerPageViewModel ViewModel => (_frame.Content as ComposerPage)?.ViewModel;

        public ComposeWindow(DraftComposeArgs args)
        {
            Title = "Open Email";

            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));

            WindowingFunctions.SetWindowIcon("Assets/appicon.ico", this);
            WindowingFunctions.CenterWindowOnScreen(this);

            Content = _frame;
            _frame.Navigate(typeof(ComposerPage), args);

            WindowingFunctions.BringToFront(this);

            Closed += WindowClosed;

            ViewModel.DismissWindow += DismissWindowRequested;
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
