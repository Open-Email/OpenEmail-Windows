using System;
using CommunityToolkit.WinUI.Controls;
using EmailValidation;
using Microsoft.UI.Xaml;
using OpenEmail.Domain.Models.Navigation;
using OpenEmail.ViewModels;


namespace OpenEmail.Views
{
    public abstract class ComposerPageAbstract : BasePage<ComposerPageViewModel> { }

    public sealed partial class ComposerPage : ComposerPageAbstract
    {
        public UIElement GetTitleBar() => ComposeTitleBar;

        public ComposerPage()
        {
            InitializeComponent();
        }

        public override void OnDisposeRequested()
        {
            base.OnDisposeRequested();

            ViewModel.OnNavigatedFrom(FrameNavigationMode.Back, null);
            ViewModel.Dispatcher = null;
        }

        public void DisposePage() => OnDisposeRequested();

        private async void AttachmentDropped(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (e.AcceptedOperation == Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy)
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Count > 0)
                {
                    foreach (var file in items)
                    {
                        await ViewModel.AddAttachmentAsync(file.Path);
                    }
                }
            }
        }

        private void AttachmentDragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private async void ReaderBeingAdded(CommunityToolkit.WinUI.Controls.TokenizingTextBox sender, CommunityToolkit.WinUI.Controls.TokenItemAddingEventArgs args)
        {
            args.Cancel = true;

            if (EmailValidator.Validate(args.TokenText))
            {
                await ViewModel.AddReaderAsync(args.TokenText);
            }
        }

        private void ReaderLostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is TokenizingTextBox senderBox)
            {
                senderBox.AddTokenItem(senderBox.Text, atEnd: true);
                senderBox.Text = string.Empty;
            }
        }
    }
}
