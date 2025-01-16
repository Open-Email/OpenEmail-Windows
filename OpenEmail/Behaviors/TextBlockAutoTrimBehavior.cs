using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace OpenEmail.Behaviors
{
    public class TextBlockAutoTrimBehavior : DependencyObject, IBehavior
    {
        public bool IsTrimmed
        {
            get { return (bool)GetValue(IsTrimmedProperty); }
            set { SetValue(IsTrimmedProperty, value); }
        }

        public static readonly DependencyProperty IsTrimmedProperty = DependencyProperty.Register(nameof(IsTrimmed), typeof(bool), typeof(TextBlockAutoTrimBehavior), new PropertyMetadata(false));

        public DependencyObject AssociatedObject { get; set; }

        public void Attach(DependencyObject associatedObject)
        {
            this.AssociatedObject = associatedObject;
            var textBlock = (TextBlock)this.AssociatedObject;
            textBlock.SizeChanged += TextBlock_SizeChanged;
        }

        private void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Height != 0)
            {
                var textBlock = (TextBlock)sender;

                this.IsTrimmed = true;

                textBlock.SizeChanged -= TextBlock_SizeChanged;

                textBlock.TextTrimming = TextTrimming.WordEllipsis;
            }
        }

        public void Detach()
        {
            var textBlock = (TextBlock)this.AssociatedObject;
            textBlock.SizeChanged -= TextBlock_SizeChanged;
        }
    }
}
