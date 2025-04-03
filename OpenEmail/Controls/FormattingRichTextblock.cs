using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Misc;

namespace OpenEmail.Controls
{
    public class FormattingRichTextblock : Control
    {
        private RichTextBlock rtb;
        private const string PART_RichTextBlock = "PART_RichTextBlock";

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(FormattingRichTextblock), new PropertyMetadata(string.Empty, new PropertyChangedCallback(OnTextChanged)));
        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(nameof(TextTrimming), typeof(TextTrimming), typeof(FormattingRichTextblock), new PropertyMetadata(TextTrimming.None));
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(FormattingRichTextblock), new PropertyMetadata(TextWrapping.NoWrap));
        public static readonly DependencyProperty MaxLinesProperty = DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(FormattingRichTextblock), new PropertyMetadata(0));
        public static readonly DependencyProperty IsTextSelectionEnabledProperty = DependencyProperty.Register(nameof(IsTextSelectionEnabled), typeof(bool), typeof(FormattingRichTextblock), new PropertyMetadata(true));

        public bool IsTextSelectionEnabled
        {
            get { return (bool)GetValue(IsTextSelectionEnabledProperty); }
            set { SetValue(IsTextSelectionEnabledProperty, value); }
        }

        public int MaxLines
        {
            get { return (int)GetValue(MaxLinesProperty); }
            set { SetValue(MaxLinesProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }


        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            rtb = GetTemplateChild(PART_RichTextBlock) as RichTextBlock;
            UpdateRTB();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormattingRichTextblock formattingRichTextblock && formattingRichTextblock.rtb != null)
            {
                formattingRichTextblock.UpdateRTB();
            }
        }

        private void UpdateRTB() => RichTextFormatter.ApplyMarkdownToRichTextblock(rtb, Text);
    }
}
