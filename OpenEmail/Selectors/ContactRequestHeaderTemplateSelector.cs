using CommunityToolkit.Mvvm.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenEmail.Selectors
{
    public class ContactRequestHeaderTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// For contact requests
        /// </summary>
        public DataTemplate RequestTemplate { get; set; }

        /// <summary>
        /// For contacts
        /// </summary>
        public DataTemplate ContactTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is IReadOnlyObservableGroup group && group.Key is bool isRequestApproved && isRequestApproved) return ContactTemplate;

            return RequestTemplate;
        }
    }
}
