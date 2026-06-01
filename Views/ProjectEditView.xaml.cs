using Holst.Models;
using Holst.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Holst.Views
{
    public partial class ProjectEditView : UserControl
    {
        private bool _isFormatting = false;

        public ProjectEditView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            EditorRichTextBox.TextChanged += EditorRichTextBox_TextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadProjectIntoEditor();
        }

        private void LoadProjectIntoEditor()
        {
            if (DataContext is not ProjectEditViewModel vm) return;
            if (vm.CurrentTextProject?.Blocks == null || vm.CurrentTextProject.Blocks.Count == 0)
            {
                EditorRichTextBox.Document = new FlowDocument();
                return;
            }

            _isFormatting = true;

            var document = new FlowDocument();
            foreach (var block in vm.CurrentTextProject.Blocks)
            {
                Paragraph paragraph;
                if (block is HeaderBlock header)
                {
                    paragraph = new Paragraph(new Run(header.Text))
                    {
                        FontSize = header.Level switch
                        {
                            1 => 34,
                            2 => 28,
                            3 => 22,
                            _ => 18
                        },
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 12, 0, 4)
                    };
                }
                else if (block is ParagraphBlock paragraphBlock)
                {
                    paragraph = new Paragraph(new Run(paragraphBlock.Text))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }
                else if (block is CodeBlock codeBlock)
                {
                    string lang = string.IsNullOrEmpty(codeBlock.Language) ? "" : codeBlock.Language;
                    paragraph = new Paragraph(new Run("```" + lang + "\n" + codeBlock.Text + "\n```"))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78)),
                        Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)),
                        Margin = new Thickness(0, 6, 0, 6),
                        Padding = new Thickness(8, 6, 8, 6)
                    };
                }
                else
                {
                    paragraph = new Paragraph(new Run(block.ToString()))
                    {
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }
                document.Blocks.Add(paragraph);
            }
            EditorRichTextBox.Document = document;

            _isFormatting = false;

            if (document.Blocks.Count > 0)
                ApplyMarkdownFormatting();
        }

        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            ApplyMarkdownFormatting();
        }

        private void ApplyMarkdownFormatting()
        {
            _isFormatting = true;

            var caretPos = EditorRichTextBox.CaretPosition;
            var blocks = EditorRichTextBox.Document.Blocks.ToList();

            foreach (var block in blocks.OfType<Paragraph>())
            {
                var entireRange = new TextRange(block.ContentStart, block.ContentEnd);
                string text = entireRange.Text;
                string trimmed = text.TrimStart();
                string trimmedEnd = text.TrimEnd();

                if (string.IsNullOrWhiteSpace(text))
                {
                    block.ClearValue(Paragraph.FontSizeProperty);
                    block.ClearValue(Paragraph.FontWeightProperty);
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.FontStyleProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.ForegroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.ClearValue(Paragraph.PaddingProperty);
                    block.ClearValue(Paragraph.MarginProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(0, 2, 0, 2);
                    continue;
                }

                if (trimmed.StartsWith("```"))
                {
                    block.FontFamily = new FontFamily("Consolas");
                    block.FontSize = 13;
                    block.FontWeight = FontWeights.Normal;
                    block.FontStyle = FontStyles.Normal;
                    block.Foreground = new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78));
                    block.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
                    block.Margin = new Thickness(0, 6, 0, 6);
                    block.Padding = new Thickness(8, 6, 8, 6);
                    block.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
                    block.BorderThickness = new Thickness(1);
                    continue;
                }

                if (trimmed.StartsWith("---") || trimmed.StartsWith("***") || trimmed.StartsWith("___"))
                {
                    block.FontSize = 1;
                    block.FontWeight = FontWeights.Normal;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
                    block.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0x4C, 0x4C));
                    block.Margin = new Thickness(0, 8, 0, 8);
                    block.Padding = new Thickness(0, 0.5, 0, 0.5);
                    block.BorderThickness = new Thickness(0);
                    continue;
                }

                if (trimmed.StartsWith("> "))
                {
                    block.FontSize = 14;
                    block.FontWeight = FontWeights.Normal;
                    block.FontStyle = FontStyles.Italic;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
                    block.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
                    block.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
                    block.BorderThickness = new Thickness(3, 0, 0, 0);
                    block.Margin = new Thickness(0, 4, 0, 4);
                    block.Padding = new Thickness(10, 4, 4, 4);

                    RebuildInlineFormatted(block, text);
                    continue;
                }

                if (trimmed.StartsWith("### "))
                {
                    block.FontSize = 22;
                    block.FontWeight = FontWeights.Bold;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(0, 10, 0, 4);
                    block.Padding = new Thickness(0);
                    RebuildHeaderContent(block, text, "### ");
                    continue;
                }

                if (trimmed.StartsWith("## "))
                {
                    block.FontSize = 28;
                    block.FontWeight = FontWeights.Bold;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(0, 12, 0, 4);
                    block.Padding = new Thickness(0);
                    RebuildHeaderContent(block, text, "## ");
                    continue;
                }

                if (trimmed.StartsWith("# "))
                {
                    block.FontSize = 34;
                    block.FontWeight = FontWeights.Bold;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(0, 14, 0, 4);
                    block.Padding = new Thickness(0);
                    RebuildHeaderContent(block, text, "# ");
                    continue;
                }

                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
                {
                    block.FontSize = EditorRichTextBox.FontSize;
                    block.FontWeight = FontWeights.Normal;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(20, 1, 0, 1);
                    block.Padding = new Thickness(0);
                    RebuildInlineFormatted(block, text);
                    continue;
                }

                if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
                {
                    block.FontSize = EditorRichTextBox.FontSize;
                    block.FontWeight = FontWeights.Normal;
                    block.FontStyle = FontStyles.Normal;
                    block.ClearValue(Paragraph.FontFamilyProperty);
                    block.ClearValue(Paragraph.BackgroundProperty);
                    block.ClearValue(Paragraph.BorderBrushProperty);
                    block.ClearValue(Paragraph.BorderThicknessProperty);
                    block.Foreground = Brushes.White;
                    block.Margin = new Thickness(20, 1, 0, 1);
                    block.Padding = new Thickness(0);
                    RebuildInlineFormatted(block, text);
                    continue;
                }

                block.ClearValue(Paragraph.FontSizeProperty);
                block.ClearValue(Paragraph.FontWeightProperty);
                block.ClearValue(Paragraph.FontStyleProperty);
                block.ClearValue(Paragraph.FontFamilyProperty);
                block.ClearValue(Paragraph.BackgroundProperty);
                block.ClearValue(Paragraph.BorderBrushProperty);
                block.ClearValue(Paragraph.BorderThicknessProperty);
                block.Foreground = Brushes.White;
                block.Margin = new Thickness(0, 2, 0, 2);
                block.Padding = new Thickness(0);
                RebuildInlineFormatted(block, text);
            }

            try
            {
                EditorRichTextBox.CaretPosition = caretPos;
            }
            catch { }

            _isFormatting = false;
        }

        private void RebuildHeaderContent(Paragraph paragraph, string rawText, string prefix)
        {
            paragraph.Inlines.Clear();
            string content = rawText;
            int idx = rawText.IndexOf(prefix, StringComparison.Ordinal);
            if (idx >= 0)
                content = rawText.Substring(idx + prefix.Length).TrimEnd('\r', '\n');
            paragraph.Inlines.Add(new Run(content) { Foreground = Brushes.White });
        }

        private void RebuildInlineFormatted(Paragraph paragraph, string text)
        {
            paragraph.Inlines.Clear();

            var pattern = @"(\*\*\*(.+?)\*\*\*)|(\*\*(.+?)\*\*)|(\*(.+?)\*)|(`(.+?)`)|(___(.+?)___)|(__.+?__)";
            int lastIndex = 0;

            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string plain = text.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Inlines.Add(new Run(plain) { Foreground = Brushes.White });
                }

                if (match.Groups[2].Success)
                {
                    paragraph.Inlines.Add(new Run(match.Groups[2].Value)
                    {
                        FontWeight = FontWeights.Bold,
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.White
                    });
                }
                else if (match.Groups[4].Success)
                {
                    paragraph.Inlines.Add(new Run(match.Groups[4].Value)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });
                }
                else if (match.Groups[6].Success)
                {
                    paragraph.Inlines.Add(new Run(match.Groups[6].Value)
                    {
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.White
                    });
                }
                else if (match.Groups[8].Success)
                {
                    paragraph.Inlines.Add(new Run(match.Groups[8].Value)
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xCE, 0x91, 0x78)),
                        Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A))
                    });
                }
                else
                {
                    string val = match.Value;
                    paragraph.Inlines.Add(new Run(val) { Foreground = Brushes.White });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                string remaining = text.Substring(lastIndex);
                paragraph.Inlines.Add(new Run(remaining) { Foreground = Brushes.White });
            }

            if (!paragraph.Inlines.Any())
            {
                paragraph.Inlines.Add(new Run(text) { Foreground = Brushes.White });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ProjectEditViewModel vm) return;

            var blocks = ParseDocumentBlocks();
            vm.UpdateDocumentBlocks(blocks);
            vm.SaveProjectCommand.Execute(null);
        }

        private List<DocumentBlock> ParseDocumentBlocks()
        {
            var blocks = new List<DocumentBlock>();

            foreach (var block in EditorRichTextBox.Document.Blocks.OfType<Paragraph>())
            {
                var textRange = new TextRange(block.ContentStart, block.ContentEnd);
                string text = textRange.Text;
                string trimmed = text.TrimStart();
                string trimmedFull = text.Trim();

                if (string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(new ParagraphBlock { Text = text });
                    continue;
                }

                if (trimmed.StartsWith("```"))
                {
                    blocks.Add(new CodeBlock { Text = trimmedFull });
                    continue;
                }

                if (trimmed.StartsWith("---") || trimmed.StartsWith("***") || trimmed.StartsWith("___"))
                {
                    blocks.Add(new ParagraphBlock { Text = trimmedFull });
                    continue;
                }

                if (trimmed.StartsWith("### "))
                {
                    blocks.Add(new HeaderBlock { Level = 3, Text = trimmedFull.Substring(4).TrimEnd('\r', '\n') });
                }
                else if (trimmed.StartsWith("## "))
                {
                    blocks.Add(new HeaderBlock { Level = 2, Text = trimmedFull.Substring(3).TrimEnd('\r', '\n') });
                }
                else if (trimmed.StartsWith("# "))
                {
                    blocks.Add(new HeaderBlock { Level = 1, Text = trimmedFull.Substring(2).TrimEnd('\r', '\n') });
                }
                else
                {
                    blocks.Add(new ParagraphBlock { Text = text });
                }
            }

            return blocks;
        }

        private void EditorRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyMarkdownFormatting();
        }
    }
}
