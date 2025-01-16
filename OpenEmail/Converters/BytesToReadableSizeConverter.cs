using System;
using Microsoft.UI.Xaml.Data;

namespace OpenEmail.Converters
{
    public class BytesToReadableSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes || value is int intBytes)
            {
                double byteValue = System.Convert.ToDouble(value);

                if (byteValue >= 1_000_000_000_000)
                    return $"{byteValue / 1_000_000_000_000:F1} TB";
                else if (byteValue >= 1_000_000_000)
                    return $"{byteValue / 1_000_000_000:F1} GB";
                else if (byteValue >= 1_000_000)
                    return $"{byteValue / 1_000_000:F1} MB";
                else if (byteValue >= 1_000)
                    return $"{byteValue / 1_000:F1} KB";
                else
                    return $"{byteValue:F0} bytes";
            }
            return "Invalid size";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
