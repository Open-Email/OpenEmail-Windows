using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;
using OpenEmail.Domain.Models;

namespace OpenEmail.Converters
{
    // All dates are ISO 8601
    public class DateConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string stringValue)
            {
                if (DateTime.TryParseExact(stringValue, CoreConstants.ISOFormat, null, DateTimeStyles.None, out DateTime dateTime))
                {
                    return dateTime;
                }

                if (DateTimeOffset.TryParseExact(stringValue, CoreConstants.ISOFormat, null, DateTimeStyles.None, out DateTimeOffset dateTimeOffset))
                {
                    return dateTimeOffset;
                }
            }

            return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime) return dateTime.ToString(CoreConstants.ISOFormat);
            if (value is DateTimeOffset dateTimeOffset) return dateTimeOffset.ToString(CoreConstants.ISOFormat);

            return string.Empty;
        }
    }
}
