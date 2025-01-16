using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace OpenEmail.Converters
{
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte[] imageBytes)
            {
                var image = new BitmapImage();
                _ = ByteArrayToBitmapImageAsync(imageBytes, image);

                return image;
            }
            else
            {
                return null;
            }
        }

        private async Task<BitmapImage> ByteArrayToBitmapImageAsync(byte[] byteArray, BitmapImage bitmapImage)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(byteArray.AsBuffer());
                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
            }

            return bitmapImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
