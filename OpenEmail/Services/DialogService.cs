using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Contracts.Application;
using OpenEmail.Dialogs;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Domain.PubSubMessages;
using OpenEmail.Helpers;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OpenEmail.Services
{
    public class DialogService : IDialogService
    {
        private SemaphoreSlim dialogSemaphore = new SemaphoreSlim(1, 1);

        public Task ShowAddNewContactDialogAsync() => HandleDialogPresentationAsync(new AddNewContactDialog());

        public async Task<bool> ShowConfirmationDialogAsync(string title, string message, WindowType windowType = WindowType.Shell)
        {
            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            AssingXamlRoot(dialog, windowType);

            var result = await dialog.ShowAsync().AsTask();

            return result == ContentDialogResult.Primary;
        }

        public Task ShowMessageAsync(string title, string message, WindowType windowType = WindowType.Shell)
        {
            var contentDialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Ok",
                DefaultButton = ContentDialogButton.Primary
            };

            AssingXamlRoot(contentDialog);

            return contentDialog.ShowAsync().AsTask();
        }

        private Window GetTargetWindow(WindowType windowType)
        {
            // First window is always the shell.
            // Second is composer.

            return windowType switch
            {
                WindowType.Shell => WindowHelper.ActiveWindows[0],
                WindowType.Composer => WindowHelper.ActiveWindows[1],
                _ => throw new NotImplementedException()
            };
        }

        private void InitializePicker(object target, WindowType windowType = WindowType.Shell)
        {
            var targetWindow = GetTargetWindow(windowType);

            nint windowHandle = WindowNative.GetWindowHandle(WindowHelper.ActiveWindows[0]);
            InitializeWithWindow.Initialize(target, windowHandle);
        }

        private void AssingXamlRoot(UIElement element, WindowType windowType = WindowType.Shell)
        {
            var targetWindow = GetTargetWindow(windowType);

            element.XamlRoot = targetWindow.Content.XamlRoot;
        }

        public async Task<byte[]> ShowProfilePicturePickerAsync()
        {
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" }
            };

            InitializePicker(picker);

            var file = await picker.PickSingleFileAsync();

            return file == null ? (new byte[0]) : File.ReadAllBytes(file.Path);
        }

        private async Task<ContentDialogResult> HandleDialogPresentationAsync(ContentDialog dialog)
        {
            await dialogSemaphore.WaitAsync();

            AssingXamlRoot(dialog);

            try
            {
                var baseContentDialog = dialog as BaseContentDialog;

                baseContentDialog.ViewModel.OnDialogOpened();

                baseContentDialog.ViewModel.HideRequested += (c, r) =>
                {
                    dialog.Hide();
                };

                var result = await dialog.ShowAsync();

                baseContentDialog?.ViewModel.OnDialogClosed();

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                dialogSemaphore.Release();
            }

            return ContentDialogResult.None;
        }

        public void ShowInfoBarMessage(string title, string message, InfoBarMessageSeverity severity, bool autoDismiss = true)
            => WeakReferenceMessenger.Default.Send(new DisplayInfoMessage(title, message, severity, autoDismiss));
    }
}
