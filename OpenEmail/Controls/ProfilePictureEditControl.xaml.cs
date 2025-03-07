using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace OpenEmail.Controls
{
    public sealed partial class ProfilePictureEditControl : UserControl
    {
        public event EventHandler DismissRequested;
        public StorageFile ImageFile { get; }
        public byte[] ResultBytes { get; private set; }

        public ProfilePictureEditControl(StorageFile imageFile)
        {
            InitializeComponent();
            ImageFile = imageFile;
        }

        private async void ControlLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ImageFile == null) return;

            await cropper.LoadImageFromFile(ImageFile);
        }

        private void ResetClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => cropper.Reset();

        private async void SaveClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var result = new MemoryStream();

            var randomAccessStream = result.AsRandomAccessStream();
            await cropper.SaveAsync(randomAccessStream, CommunityToolkit.WinUI.Controls.BitmapFileFormat.Jpeg, false);

            result.Position = 0;
            ResultBytes = result.ToArray();

            Dismiss();
        }

        private void Dismiss() => DismissRequested?.Invoke(this, null);

        private void CancelClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}
