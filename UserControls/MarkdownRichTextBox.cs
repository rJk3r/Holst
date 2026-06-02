using Holst.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Holst.UserControls
{
    /// <summary>
    /// RichTextBox с live Markdown-форматированием на уровне абзацев и inline-элементов.
    /// Форматирование применяется через TextRange.ApplyPropertyValue — без пересоздания Runs,
    /// что позволяет курсору оставаться на месте и текст вводиться корректно.
    /// </summary>
    public class MarkdownRichTextBox : RichTextBox
    {
        private bool _isFormatting = false;

        public static readonly DependencyProperty BlocksSourceProperty =
            DependencyProperty.Register(
                nameof(BlocksSource),
                typeof(System.Collections.Generic.IList<DocumentBlock>),
                typeof(MarkdownRichTextBox),
                new PropertyMetadata(null, OnBlocksSourceChanged));

        public System.Collections.Generic.IList<DocumentBlock>? BlocksSource
        {
            get => (System.Collections.Generic.IList<DocumentBlock>?)GetValue(BlocksSourceProperty);
            set => SetValue(BlocksSourceProperty, value);
        }

        public MarkdownRichTextBox()
        {
            Loaded += OnLoaded;
            TextChanged += OnTextChanged;
            LostFocus += OnLostFocus;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private static void OnBlocksSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownRichTextBox rtb)
                rtb.LoadBlocksIntoDocument();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadBlocksIntoDocument();
        }

        /// <summary>
        /// Форматируем текущий абзац по Enter/Space для более быстрой реакции на префиксы (#, -, > и т.д.)
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        var paragraph = CaretPosition?.Paragraph;
                        if (paragraph != null)
                            FormatParagraph(paragraph);
                    }),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;

            var paragraph = CaretPosition?.Paragraph;
            if (paragraph == null) return;

            _isFormatting = true;
            try
            {
                FormatParagraph(paragraph);
            }
            finally
            {
                _isFormatting = false;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            FormatAllParagraphs();
        }

        private void FormatAllParagraphs()
        {
            if (Document == null) return;

            _isFormatting = true;
            try
            {
                foreach (var block in Document.Blocks.OfType<Paragraph>())
                    FormatParagraph(block);
            }
            finally
            {
                _isFormatting = false;
            }
        }

        private void LoadBlocksIntoDocument()
        {
            var blocks = BlocksSource;
            if (blocks == null || blocks.Count == 0)
            {
                Document = new FlowDocument();
                return;
            }

            _isFormatting = true;
            var document = new FlowDocument();

            foreach (var block in blocks)
            {
                Paragraph paragraph;

                if (block is HeaderBlock header)
                {
                    string prefix = header.Level switch { 1 => "# ", 2 => "## ", 3 => "### ", _ => "" };
                    paragraph = new Paragraph(new Run(prefix + header.Text))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 12, 0, 4)
                    };
                }
                else if (block is ParagraphBlock pb)
                {
                    paragraph = new Paragraph(new Run(pb.Text))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }
                else if (block is CodeBlock code)
                {
                    string lang = string.IsNullOrEmpty(code.Language) ? "" : code.Language;
                    paragraph = new Paragraph(new Run("```" + lang + "\n" + code.Text + "\n```"))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }
                else
                {
                    paragraph = new Paragraph(new Run(block.ToString() ?? string.Empty))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }

                document.Blocks.Add(paragraph);
            }

            Document = document;
            _isFormatting = false;
            FormatAllParagraphs();
        }

        #region Paragraph formatting

        private static void FormatParagraph(Paragraph paragraph)
        {
            var textRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            string rawText = textRange.Text;
            string text = rawText.TrimEnd('\r', '\n');
            string trimmed = text.TrimStart();

            if (string.IsNullOrWhiteSpace(text))
            {
                ResetParagraphProperties(paragraph);
                ResetInlineProperties(paragraph);
                return;
            }

            // Code block
            if (trimmed.StartsWith("```"))
            {
                ApplyCodeBlockProperties(paragraph);
                ResetInlineProperties(paragraph);
                return;
            }

            // Horizontal rule
            if (trimmed.StartsWith("---") || trimmed.StartsWith("***") || trimmed.StartsWith("___"))
            {
                ApplyHorizontalRuleProperties(paragraph);
                ResetInlineProperties(paragraph);
                return;
            }

            // Blockquote
            if (trimmed.StartsWith("> "))
            {
                ApplyQuoteProperties(paragraph);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            // Headers
            if (trimmed.StartsWith("### "))
            {
                ApplyHeaderProperties(paragraph, 3);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            if (trimmed.StartsWith("## "))
            {
                ApplyHeaderProperties(paragraph, 2);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            if (trimmed.StartsWith("# "))
            {
                ApplyHeaderProperties(paragraph, 1);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            // Unordered list
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
            {
                ApplyListProperties(paragraph);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            // Ordered list
            if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                ApplyOrderedListProperties(paragraph);
                ApplyInlineFormatting(paragraph, text);
                return;
            }

            // Normal paragraph
            ApplyNormalProperties(paragraph);
            ApplyInlineFormatting(paragraph, text);
        }

        private static void ResetParagraphProperties(Paragraph paragraph)
        {
            paragraph.ClearValue(Paragraph.FontSizeProperty);
            paragraph.ClearValue(Paragraph.FontWeightProperty);
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.ClearValue(Paragraph.FontStyleProperty);
            paragraph.ClearValue(Paragraph.BackgroundProperty);
            paragraph.ClearValue(Paragraph.ForegroundProperty);
            paragraph.ClearValue(Paragraph.BorderBrushProperty);
            paragraph.ClearValue(Paragraph.BorderThicknessProperty);
            paragraph.ClearValue(Paragraph.PaddingProperty);
            paragraph.ClearValue(Paragraph.MarginProperty);
            paragraph.Foreground = Brushes.White;
            paragraph.Margin = new Thickness(0, 2, 0, 2);
        }

        private static void ApplyNormalProperties(Paragraph paragraph)
        {
            paragraph.ClearValue(Paragraph.FontSizeProperty);
            paragraph.ClearValue(Paragraph.FontWeightProperty);
            paragraph.ClearValue(Paragraph.FontStyleProperty);
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.ClearValue(Paragraph.BackgroundProperty);
            paragraph.ClearValue(Paragraph.BorderBrushProperty);
            paragraph.ClearValue(Paragraph.BorderThicknessProperty);
            paragraph.Foreground = Brushes.White;
            paragraph.Margin = new Thickness(0, 2, 0, 2);
            paragraph.Padding = new Thickness(0);
        }

        private static void ApplyHeaderProperties(Paragraph paragraph, int level)
        {
            paragraph.FontSize = level switch
            {
                1 => 34,
                2 => 28,
                3 => 22,
                _ => 18
            };
            paragraph.FontWeight = FontWeights.Bold;
            paragraph.FontStyle = FontStyles.Normal;
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.ClearValue(Paragraph.BackgroundProperty);
            paragraph.ClearValue(Paragraph.BorderBrushProperty);
            paragraph.ClearValue(Paragraph.BorderThicknessProperty);
            paragraph.Foreground = Brushes.White;
            paragraph.Margin = level switch
            {
                1 => new Thickness(0, 14, 0, 4),
                2 => new Thickness(0, 12, 0, 4),
                3 => new Thickness(0, 10, 0, 4),
                _ => new Thickness(0, 8, 0, 4)
            };
            paragraph.Padding = new Thickness(0);
        }

        private static void ApplyCodeBlockProperties(Paragraph paragraph)
        {
            paragraph.FontFamily = new FontFamily("Consolas");
            paragraph.FontSize = 13;
            paragraph.FontWeight = FontWeights.Normal;
            paragraph.FontStyle = FontStyles.Normal;
            paragraph.Foreground = new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78));
            paragraph.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            paragraph.Margin = new Thickness(0, 6, 0, 6);
            paragraph.Padding = new Thickness(8, 6, 8, 6);
            paragraph.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
            paragraph.BorderThickness = new Thickness(1);
        }

        private static void ApplyHorizontalRuleProperties(Paragraph paragraph)
        {
            paragraph.FontSize = 1;
            paragraph.FontWeight = FontWeights.Normal;
            paragraph.FontStyle = FontStyles.Normal;
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
            paragraph.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
            paragraph.Margin = new Thickness(0, 8, 0, 8);
            paragraph.Padding = new Thickness(0, 0.5, 0, 0.5);
            paragraph.BorderThickness = new Thickness(0);
        }

        private static void ApplyQuoteProperties(Paragraph paragraph)
        {
            paragraph.FontSize = 14;
            paragraph.FontWeight = FontWeights.Normal;
            paragraph.FontStyle = FontStyles.Italic;
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            paragraph.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            paragraph.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
            paragraph.BorderThickness = new Thickness(3, 0, 0, 0);
            paragraph.Margin = new Thickness(0, 4, 0, 4);
            paragraph.Padding = new Thickness(10, 4, 4, 4);
        }

        private static void ApplyListProperties(Paragraph paragraph)
        {
            paragraph.ClearValue(Paragraph.FontSizeProperty);
            paragraph.FontWeight = FontWeights.Normal;
            paragraph.FontStyle = FontStyles.Normal;
            paragraph.ClearValue(Paragraph.FontFamilyProperty);
            paragraph.ClearValue(Paragraph.BackgroundProperty);
            paragraph.ClearValue(Paragraph.BorderBrushProperty);
            paragraph.ClearValue(Paragraph.BorderThicknessProperty);
            paragraph.Foreground = Brushes.White;
            paragraph.Margin = new Thickness(20, 1, 0, 1);
            paragraph.Padding = new Thickness(0);
        }

        private static void ApplyOrderedListProperties(Paragraph paragraph)
        {
            ApplyListProperties(paragraph);
        }

        #endregion

        #region Inline formatting

        /// <summary>
        /// Сбрасывает все локально установленные inline-свойства в абзаце к унаследованным значениям.
        /// </summary>
        private static void ResetInlineProperties(Paragraph paragraph)
        {
            var range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            range.ClearAllProperties();
        }

        /// <summary>
        /// Применяет inline Markdown-форматирование (жирный, курсив, код) через TextRange.
        /// </summary>
        private static void ApplyInlineFormatting(Paragraph paragraph, string text)
        {
            string clean = text.TrimEnd('\r', '\n');

            // Сначала сбрасываем inline-свойства всего абзаца, чтобы старый формат не остался
            // после удаления markdown-символов.
            var entireRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            entireRange.ClearAllProperties();
            // После ClearAllProperties inline-элементы наследуют paragraph-level стили
            // (FontSize, Foreground, Background и т.д.), заданные в FormatParagraph.

            // Паттерны: ***bi***, **b**, *i*, `code`
            const string pattern = @"(\*\*\*(.+?)\*\*\*)|(\*\*(.+?)\*\*)|(\*(.+?)\*)|(`(.+?)`)";
            var matches = Regex.Matches(clean, pattern);

            foreach (Match match in matches)
            {
                var startPtr = GetTextPointerAtOffset(paragraph, match.Index);
                var endPtr = GetTextPointerAtOffset(paragraph, match.Index + match.Length);
                if (startPtr == null || endPtr == null || startPtr.CompareTo(endPtr) >= 0)
                    continue;

                var range = new TextRange(startPtr, endPtr);

                if (match.Groups[2].Success) // ***bold italic***
                {
                    range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                }
                else if (match.Groups[4].Success) // **bold**
                {
                    range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                }
                else if (match.Groups[6].Success) // *italic*
                {
                    range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                }
                else if (match.Groups[8].Success) // `code`
                {
                    range.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Consolas"));
                    range.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78)));
                    range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)));
                }
            }
        }

        /// <summary>
        /// Возвращает TextPointer на указанный символьный offset от начала содержимого абзаца.
        /// Учитывает только текстовые символы (Run'ы); служебные границы элементов пропускаются.
        /// </summary>
        private static TextPointer? GetTextPointerAtOffset(Paragraph paragraph, int charOffset)
        {
            if (charOffset < 0) return null;

            var pointer = paragraph.ContentStart;
            int current = 0;

            while (pointer != null && pointer.CompareTo(paragraph.ContentEnd) < 0)
            {
                if (current == charOffset)
                    return pointer;

                switch (pointer.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.Text:
                        int runLength = pointer.GetTextRunLength(LogicalDirection.Forward);
                        if (current + runLength > charOffset)
                        {
                            return pointer.GetPositionAtOffset(charOffset - current, LogicalDirection.Forward);
                        }
                        current += runLength;
                        pointer = pointer.GetPositionAtOffset(runLength, LogicalDirection.Forward);
                        break;

                    default:
                        // ElementStart, ElementEnd, EmbeddedElement, None — пропускаем без увеличения счётчика,
                        // т.к. они не дают символов в TextRange.Text.
                        pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                        break;
                }
            }

            return pointer; // ContentEnd или null
        }

        #endregion
    }
}
