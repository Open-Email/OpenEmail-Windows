using System;
using System.ComponentModel;
using OpenEmail.Contracts.Application;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace OpenEmail.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public T Get<T>(string key, T defaultValue = default)
            => GetInternal(key, ApplicationData.Current.LocalSettings.Values, defaultValue);

        public void Set(string key, object value)
            => SetInternal(key, value, ApplicationData.Current.LocalSettings.Values);

        private T GetInternal<T>(string key, IPropertySet collection, T defaultValue = default)
        {
            if (collection.ContainsKey(key))
            {
                var value = collection[key]?.ToString();

                if (typeof(T).IsEnum)
                    return (T)Enum.Parse(typeof(T), value);

                if (typeof(T) == typeof(Guid?) && Guid.TryParse(value, out Guid guidResult))
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
                }

                if (typeof(T) == typeof(TimeSpan))
                {
                    return (T)(object)TimeSpan.Parse(value);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        private void SetInternal(string key, object value, IPropertySet collection)
            => collection[key] = value?.ToString();
    }
}
