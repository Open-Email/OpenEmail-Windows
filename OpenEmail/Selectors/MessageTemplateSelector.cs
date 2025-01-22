using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.ViewModels.Data;

namespace OpenEmail.Selectors
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MessageTemplate { get; set; }
        public DataTemplate ThreadMessageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is MessageViewModel messageViewModel) return MessageTemplate;
            if (item is MessageThreadViewModel messageThreadViewModel) return ThreadMessageTemplate;

            return base.SelectTemplateCore(item, container);
        }
    }
}
