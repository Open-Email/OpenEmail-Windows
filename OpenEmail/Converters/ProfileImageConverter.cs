using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenEmail.Contracts.Configuration;
using Windows.Storage;

namespace OpenEmail.Converters
{
    /// <summary>
    /// This converter will help resolving profile image from the given address and thumbnail.
    /// It's important because Image control locks the image file when used directly with ms-appdata://.
    /// </summary>
    public class ProfileImageConverter : IValueConverter
    {
        private IApplicationConfiguration _applicationConfiguration = App.Current.Services.GetService<IApplicationConfiguration>();

        /// <summary>
        /// Replace avatar if profile picture doesn't exist with pre-defined image.
        /// </summary>
        public bool UseEmptyAvatarIfNoImage { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var fileName = value.ToString();
            var fullPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, "ProfileData", fileName);

            var bitmap = new BitmapImage();

            if (File.Exists(fullPath))
            {
                using var fs = new FileStream(fullPath, FileMode.Open);

                _ = bitmap.SetSourceAsync(fs.AsRandomAccessStream());
            }
            else if (UseEmptyAvatarIfNoImage)
            {
                _ = LoadEmptyAvatarAsync(bitmap);
            }

            return bitmap;
        }

        private async Task LoadEmptyAvatarAsync(BitmapImage bitmapImage)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/noavatar.png"));

                using var stream = await file.OpenReadAsync();
                await bitmapImage.SetSourceAsync(stream);
            }
            catch
            {
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
