using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Selectors
{
    public class MessageUploadProgressTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Uploading { get; set; }
        public DataTemplate Completed { get; set; }
        public DataTemplate Failed { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is MessageUploadProgress progress)
            {
                switch (progress.Status)
                {
                    case MessageStatus.Uploading:
                        return Uploading;
                    case MessageStatus.Completed:
                        return Completed;
                    case MessageStatus.UploadingFailed:
                        return Failed;
                }
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
