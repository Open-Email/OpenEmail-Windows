using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace OpenEmail.Misc
{
    public static class RichTextFormatter
    {
        /// <summary>
        /// Converts the text from a rich edit box into Markdown format, preserving text styles like bold, italic, and
        /// strikethrough.
        /// </summary>
        /// <param name="EditBox">The rich edit box containing formatted text to be converted into Markdown.</param>
        /// <returns>A string representing the Markdown formatted text derived from the rich edit box.</returns>
        public static string GetMarkdownFromRichEditBox(RichEditBox EditBox)
        {
            EditBox.Document.GetText(TextGetOptions.None, out string plainText);
            plainText = plainText.TrimEnd('\r');

            var sb = new StringBuilder();
            int length = plainText.Length;
            var range = EditBox.Document.GetRange(0, 0);

            bool currentBold = false;
            bool currentItalic = false;
            bool currentStrike = false;

            for (int i = 0; i < length; i++)
            {
                range.SetRange(i, i + 1);
                bool isBold = range.CharacterFormat.Bold == FormatEffect.On;
                bool isItalic = range.CharacterFormat.Italic == FormatEffect.On;
                bool isStrike = range.CharacterFormat.Strikethrough == FormatEffect.On;

                // Check for format changes and add opening tags
                if (!currentBold && isBold) { sb.Append('*'); currentBold = true; }
                if (!currentItalic && isItalic) { sb.Append('_'); currentItalic = true; }
                if (!currentStrike && isStrike) { sb.Append('~'); currentStrike = true; }

                // Check for format endings and add closing tags (in reverse order of opening)
                if (currentStrike && !isStrike) { sb.Append('~'); currentStrike = false; }
                if (currentItalic && !isItalic) { sb.Append('_'); currentItalic = false; }
                if (currentBold && !isBold) { sb.Append('*'); currentBold = false; }

                sb.Append(plainText[i]);
            }

            // Close any remaining tags at the end of text
            if (currentStrike) sb.Append('~');
            if (currentItalic) sb.Append('_');
            if (currentBold) sb.Append('*');

            return sb.ToString();
        }

        /// <summary>
        /// Applies Markdown formatting to text in a RichEditBox by parsing specific Markdown symbols.  
        /// </summary>
        /// <param name="richEditBox">The control where the formatted text will be displayed after processing.</param>
        /// <param name="markdownText">The input string containing Markdown syntax that needs to be converted to formatted text.</param>
        public static void ApplyMarkdownToRichEditBox(RichEditBox richEditBox, string markdownText)
        {
            // Define regex patterns and corresponding format actions
            var patterns = new Dictionary<string, Action<ITextRange>>
            {
                {@"\*(.*?)\*", range => range.CharacterFormat.Bold = FormatEffect.On}, // *bold*
                {@"_(.*?)_", range => range.CharacterFormat.Italic = FormatEffect.On}, // _italic_
                {@"~(.*?)~", range => range.CharacterFormat.Strikethrough = FormatEffect.On} // ~strikethrough~
            };

            string cleanText = markdownText;
            var formattingPositions = new List<(int Start, int Length, Action<ITextRange> ApplyFormat)>();

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(cleanText, pattern.Key);
                int offset = 0;

                foreach (Match match in matches)
                {
                    string matchText = match.Groups[1].Value; // Extract text inside the symbols
                    int start = match.Index - offset; // Adjust start position
                    int length = matchText.Length;

                    formattingPositions.Add((start, length, pattern.Value));

                    // Remove Markdown symbols from the cleanText
                    cleanText = cleanText.Remove(match.Index - offset, 1) // Remove opening symbol
                                         .Remove(match.Index + length - offset, 1); // Remove closing symbol
                    offset += 2; // Account for two removed characters
                }
            }

            // Set the cleaned text in RichEditBox
            richEditBox.Document.SetText(TextSetOptions.None, cleanText);

            // Apply formatting
            foreach (var (start, length, applyFormat) in formattingPositions)
            {
                var range = richEditBox.Document.GetRange(start, start + length);
                applyFormat(range);
            }
        }

        public static void ApplyMarkdownToRichTextblock(RichTextBlock richTextBlock, string markdownText)
        {
            // Define regex patterns and corresponding format actions
            var patterns = new Dictionary<string, Action<Run>>
            {
                {@"\*(.*?)\*", run => run.FontWeight = FontWeights.Bold}, // *bold*
                {@"_(.*?)_", run => run.FontStyle = Windows.UI.Text.FontStyle.Italic}, // _italic_
                {@"~(.*?)~", run => run.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough} // ~strikethrough~
            };

            string cleanText = markdownText;
            var formattingPositions = new List<(int Start, int Length, Action<Run> ApplyFormat)>();

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(cleanText, pattern.Key);
                int offset = 0;

                foreach (Match match in matches)
                {
                    string matchText = match.Groups[1].Value;
                    int start = match.Index - offset;
                    int length = matchText.Length;

                    formattingPositions.Add((start, length, pattern.Value));

                    cleanText = cleanText.Remove(match.Index - offset, 1)
                                       .Remove(match.Index + length - offset, 1);
                    offset += 2;
                }
            }

            richTextBlock.Blocks.Clear();
            var paragraph = new Paragraph();

            if (formattingPositions.Count == 0)
            {
                paragraph.Inlines.Add(new Run { Text = cleanText });
            }
            else
            {
                int currentPos = 0;
                var sortedPositions = formattingPositions.OrderBy(p => p.Start).ToList();

                foreach (var (start, length, applyFormat) in sortedPositions)
                {
                    // Add any text before the formatted section
                    if (start > currentPos)
                    {
                        paragraph.Inlines.Add(new Run { Text = cleanText.Substring(currentPos, start - currentPos) });
                    }

                    // Add the formatted text
                    var formattedRun = new Run { Text = cleanText.Substring(start, length) };
                    applyFormat(formattedRun);
                    paragraph.Inlines.Add(formattedRun);

                    currentPos = start + length;
                }

                // Add any remaining text
                if (currentPos < cleanText.Length)
                {
                    paragraph.Inlines.Add(new Run { Text = cleanText.Substring(currentPos) });
                }
            }

            richTextBlock.Blocks.Add(paragraph);
        }
    }
}
