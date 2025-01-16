using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Selectors
{
    public class ProfilePropertyTemplateSelector : DataTemplateSelector
    {
        private const string True = "Yes";
        private const string False = "No";

        public DataTemplate DateTimeTemplate { get; set; }
        public DataTemplate DateTimeOffsetTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item == null || item is not ProfileAttribute profileAttribute) throw new Exception("Don't pass null values here. They can't be represented.");

            var itemString = profileAttribute.Value;

            if (DateTimeOffset.TryParse(itemString, out DateTimeOffset dateTimeOffset)) return DateTimeOffsetTemplate;
            if (DateTime.TryParse(itemString, out DateTime dateTime)) return DateTimeTemplate;
            if (bool.TryParse(itemString, out bool boolean) || itemString == True || itemString == False) return BooleanTemplate;

            return StringTemplate;
        }
    }
}
