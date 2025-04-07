using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace OpenEmail.Controls
{
    public class FormattingRichEditBox : RichEditBox
    {
        private bool isUpdating = false;
        private Stopwatch sw = new Stopwatch();

        public FormattingRichEditBox()
        {
            TextChanged += MarkdownRichEditBox_TextChanged;
            SelectionChanged += FormattingRichEditBox_SelectionChanged;
        }

        private void FormattingRichEditBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            base.OnTapped(e);

            var tappedPoint = e.GetPosition(this);
            var textRange = Document.GetRangeFromPoint(tappedPoint, PointOptions.Start);
            textRange.StartOf(TextRangeUnit.Link, true);

            var mylink = textRange.Link;
        }

        private void MarkdownRichEditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (isUpdating) return;

            TextChanged -= MarkdownRichEditBox_TextChanged;
            sw.Restart();
            isUpdating = true;

            // Store current selection
            int selectionStart = Document.Selection.StartPosition;
            int selectionLength = Document.Selection.Length;

            Document.GetText(TextGetOptions.None, out string text);

            // First, reset all formatting to normal
            var fullRange = Document.GetRange(0, text.Length);
            fullRange.CharacterFormat.Bold = FormatEffect.Off;
            fullRange.CharacterFormat.Strikethrough = FormatEffect.Off;
            fullRange.CharacterFormat.Italic = FormatEffect.Off;
            fullRange.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Black; // Reset URL color
            fullRange.CharacterFormat.Underline = UnderlineType.None;           // Reset URL underline

            // Apply bold formatting first (double stars)
            ApplyFormatting(text, @"\*\*(.*?)\*\*", range => range.CharacterFormat.Bold = FormatEffect.On);

            // Apply italic formatting (single stars, but not if part of double stars)
            ApplyFormatting(text, @"(?<!\*)\*(?!\*)(.*?)(?<!\*)\*(?!\*)", range => range.CharacterFormat.Italic = FormatEffect.On);

            // Apply strikethrough formatting
            ApplyFormatting(text, @"~~(.*?)~~", range => range.CharacterFormat.Strikethrough = FormatEffect.On);

            // Apply URL formatting
            var urlPattern = @"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})";
            var matches = Regex.Matches(text, urlPattern);

            // Process each word for potential URL formatting
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var startIndex = text.IndexOf(word);
                if (startIndex >= 0)
                {
                    var range = Document.GetRange(startIndex, startIndex + word.Length);
                    if (Regex.IsMatch(word, urlPattern))
                    {
                        range.CharacterFormat.ForegroundColor = Windows.UI.Color.FromArgb(255, 0, 120, 215);
                        range.CharacterFormat.Underline = UnderlineType.Single;
                        
                        string linkUrl = word;
                        if (!word.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                            !word.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            linkUrl = "https://" + word;
                        }
                        try
                        {
                            range.Link = linkUrl;
                        }
                        catch
                        {
                            // If setting the link fails, just keep the visual formatting
                        }
                    }
                }
            }

            // Restore selection
            Document.Selection.SetRange(selectionStart, selectionStart + selectionLength);

            isUpdating = false;
            Debug.WriteLine($"Update took {sw.ElapsedMilliseconds} ms");
            sw.Stop();
            TextChanged += MarkdownRichEditBox_TextChanged;
        }

        private void ApplyFormatting(string text, string pattern, Action<ITextRange> formatAction)
        {
            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                var range = Document.GetRange(match.Index, match.Index + match.Length);
                formatAction(range);
            }
        }
    }
}
