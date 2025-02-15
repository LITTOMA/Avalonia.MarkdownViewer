using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MarkdownViewer.Core.Controls;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Services;
using System.IO;

namespace MarkdownViewer.Core.Implementations
{
    public class AvaloniaMarkdownRenderer : IMarkdownRenderer
    {
        private readonly FontFamily _defaultFontFamily = FontFamily.Default;
        private readonly double _baseFontSize = 14;
        private readonly IImageCache _imageCache;
        private readonly ILogger _logger;

        public event EventHandler<string>? LinkClicked;

        public AvaloniaMarkdownRenderer(
            IImageCache imageCache,
            ILogger<AvaloniaMarkdownRenderer> logger
        )
        {
            _imageCache = imageCache;
            _logger = logger;
        }

        public Control RenderDocument(string markdown)
        {
            var parser = new MarkdigParser();
            var elements = parser.ParseTextAsync(markdown).ToBlockingEnumerable();

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

            foreach (var element in elements)
            {
                var control = RenderElement(element);
                panel.Children.Add(control);
            }

            return panel;
        }

        private Control RenderElement(MarkdownElement element)
        {
            return element switch
            {
                HeadingElement heading => RenderHeading(heading),
                ParagraphElement paragraph => RenderParagraph(paragraph),
                CodeBlockElement codeBlock => RenderCodeBlock(codeBlock),
                ListElement list => RenderList(list),
                QuoteElement quote => RenderQuote(quote),
                ImageElement image => RenderImage(image),
                LinkElement link => RenderLink(link),
                TableElement table => RenderTable(table),
                EmphasisElement emphasis => RenderEmphasis(emphasis),
                _ => new TextBlock { Text = "Unsupported element" }
            };
        }

        public void UpdateElement(IControl control, MarkdownElement element)
        {
            switch (element)
            {
                case HeadingElement heading when control is TextBlock textBlock:
                    UpdateHeading(textBlock, heading);
                    break;
                case ParagraphElement paragraph when control is TextBlock textBlock:
                    UpdateParagraph(textBlock, paragraph);
                    break;
                case CodeBlockElement codeBlock when control is TextBox textBox:
                    UpdateCodeBlock(textBox, codeBlock);
                    break;
                case ListElement list when control is StackPanel panel:
                    UpdateList(panel, list);
                    break;
                case QuoteElement quote when control is Border border:
                    UpdateQuote(border, quote);
                    break;
                case ImageElement image when control is Image img:
                    UpdateImage(img, image);
                    break;
                case LinkElement link when control is Button btn:
                    UpdateLink(btn, link);
                    break;
                case TableElement table when control is Grid grid:
                    UpdateTable(grid, table);
                    break;
                case EmphasisElement emphasis when control is TextBlock textBlock:
                    UpdateEmphasis(textBlock, emphasis);
                    break;
            }
        }

        private Control RenderHeading(HeadingElement heading)
        {
            var textBlock = new TextBlock
            {
                Text = heading.Text,
                FontFamily = _defaultFontFamily,
                FontWeight = FontWeight.Bold,
                FontSize = GetHeadingFontSize(heading.Level),
                Margin = new Thickness(0, heading.Level == 1 ? 20 : 15, 0, 10)
            };
            return textBlock;
        }

        private Control RenderParagraph(ParagraphElement paragraph)
        {
            var textBlock = new TextBlock
            {
                FontFamily = _defaultFontFamily,
                FontSize = _baseFontSize,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            if (paragraph.Inlines != null)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline == null)
                        continue;

                    switch (inline)
                    {
                        case Elements.TextElement text:
                            textBlock.Inlines?.Add(new Run { Text = text.Text ?? string.Empty });
                            break;
                        case EmphasisElement emphasis:
                            if (emphasis.IsStrong)
                            {
                                var bold = new Bold
                                {
                                    Inlines = { new Run { Text = emphasis.Text ?? string.Empty } }
                                };
                                textBlock.Inlines?.Add(bold);
                            }
                            else
                            {
                                var italic = new Italic
                                {
                                    Inlines = { new Run { Text = emphasis.Text ?? string.Empty } }
                                };
                                textBlock.Inlines?.Add(italic);
                            }
                            break;
                        case LinkElement link:
                            var span = new Span
                            {
                                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 255)),
                                TextDecorations = TextDecorations.Underline
                            };
                            span.Inlines?.Add(new Run { Text = link.Text ?? string.Empty });
                            textBlock.Inlines?.Add(span);
                            break;
                    }
                }
            }

            return textBlock;
        }

        private Control RenderCodeBlock(CodeBlockElement codeBlock)
        {
            var textBox = new TextBox
            {
                Text = codeBlock.Code,
                FontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace"),
                FontSize = _baseFontSize,
                IsReadOnly = true,
                AcceptsReturn = true,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                BorderThickness = new Thickness(0)
            };
            return textBox;
        }

        private void UpdateHeading(TextBlock textBlock, HeadingElement heading)
        {
            textBlock.Text = heading.Text;
            textBlock.FontSize = GetHeadingFontSize(heading.Level);
        }

        private void UpdateParagraph(TextBlock textBlock, ParagraphElement paragraph)
        {
            if (textBlock.Inlines == null)
                return;

            textBlock.Inlines.Clear();
            foreach (var inline in paragraph.Inlines)
            {
                if (inline == null)
                    continue;

                switch (inline)
                {
                    case Elements.TextElement text:
                        textBlock.Inlines.Add(new Run { Text = text.Text ?? string.Empty });
                        break;
                    case EmphasisElement emphasis:
                        if (emphasis.IsStrong)
                        {
                            var bold = new Bold
                            {
                                Inlines = { new Run { Text = emphasis.Text ?? string.Empty } }
                            };
                            textBlock.Inlines.Add(bold);
                        }
                        else
                        {
                            var italic = new Italic
                            {
                                Inlines = { new Run { Text = emphasis.Text ?? string.Empty } }
                            };
                            textBlock.Inlines.Add(italic);
                        }
                        break;
                    case LinkElement link:
                        var span = new Span
                        {
                            Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 255)),
                            TextDecorations = TextDecorations.Underline
                        };
                        span.Inlines?.Add(new Run { Text = link.Text ?? string.Empty });
                        textBlock.Inlines.Add(span);
                        break;
                }
            }
        }

        private void UpdateCodeBlock(TextBox textBox, CodeBlockElement codeBlock)
        {
            textBox.Text = codeBlock.Code;
        }

        private double GetHeadingFontSize(int level)
        {
            return level switch
            {
                1 => _baseFontSize * 2.0,
                2 => _baseFontSize * 1.7,
                3 => _baseFontSize * 1.4,
                4 => _baseFontSize * 1.2,
                5 => _baseFontSize * 1.1,
                _ => _baseFontSize
            };
        }

        private Control RenderList(ListElement list)
        {
            var panel = new StackPanel { Spacing = 5, Margin = new Thickness(0, 0, 0, 10) };

            if (list.Items != null)
            {
                foreach (var item in list.Items)
                {
                    if (item == null)
                        continue;

                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(item.Level * 20, 0, 0, 0)
                    };

                    var bullet = new TextBlock
                    {
                        Text = list.IsOrdered ? $"{list.Items.IndexOf(item) + 1}." : "•",
                        Width = 20,
                        TextAlignment = TextAlignment.Right,
                        Margin = new Thickness(0, 0, 5, 0)
                    };

                    var content = new TextBlock
                    {
                        Text = item.Text ?? string.Empty,
                        TextWrapping = TextWrapping.Wrap
                    };

                    itemPanel.Children?.Add(bullet);
                    itemPanel.Children?.Add(content);
                    panel.Children?.Add(itemPanel);
                }
            }

            return panel;
        }

        private Control RenderQuote(QuoteElement quote)
        {
            var textBlock = new TextBlock
            {
                Text = quote.Text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 108, 108))
            };

            return new Border
            {
                Child = textBlock,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                BorderThickness = new Thickness(4, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(249, 249, 249)),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };
        }

        private Control RenderImage(ImageElement image)
        {
            var img = new Image
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                MaxHeight = 400,
                Margin = new Thickness(0, 0, 0, 10)
            };

            LoadImageAsync(img, image.Source);
            return img;
        }

        private Control RenderLink(LinkElement link)
        {
            var run = new Run
            {
                Text = link.Text ?? string.Empty,
                TextDecorations = TextDecorations.Underline,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 255))
            };

            var textBlock = new TextBlock();
            if (textBlock.Inlines != null)
            {
                textBlock.Inlines.Add(run);
            }

            var button = new Button
            {
                Content = textBlock,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            button.Click += (s, e) => OnLinkClicked(link.Url ?? string.Empty);
            return button;
        }

        private void UpdateList(StackPanel panel, ListElement list)
        {
            if (panel.Children == null || list.Items == null)
                return;

            panel.Children.Clear();
            foreach (var item in list.Items)
            {
                if (item == null)
                    continue;

                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(item.Level * 20, 0, 0, 0)
                };

                var bullet = new TextBlock
                {
                    Text = list.IsOrdered ? $"{list.Items.IndexOf(item) + 1}." : "•",
                    Width = 20,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 0, 5, 0)
                };

                var content = new TextBlock
                {
                    Text = item.Text ?? string.Empty,
                    TextWrapping = TextWrapping.Wrap
                };

                if (itemPanel.Children != null)
                {
                    itemPanel.Children.Add(bullet);
                    itemPanel.Children.Add(content);
                }

                panel.Children.Add(itemPanel);
            }
        }

        private void UpdateQuote(Border border, QuoteElement quote)
        {
            if (border.Child is TextBlock textBlock)
            {
                textBlock.Text = quote.Text;
            }
        }

        private void UpdateImage(Image img, ImageElement image)
        {
            LoadImageAsync(img, image.Source);
        }

        private void UpdateLink(Button btn, LinkElement link)
        {
            if (btn.Content is TextBlock textBlock && textBlock.Inlines != null)
            {
                textBlock.Inlines.Clear();
                var run = new Run
                {
                    Text = link.Text ?? string.Empty,
                    TextDecorations = TextDecorations.Underline,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 255))
                };
                textBlock.Inlines.Add(run);
            }
        }

        private async void LoadImageAsync(Image img, string source)
        {
            if (img == null)
                return;

            try
            {
                var imageData = await _imageCache.GetImageAsync(source);
                if (imageData != null)
                {
                    using var stream = new MemoryStream(imageData);
                    var bitmap = new Bitmap(stream);
                    img.Source = bitmap;
                }
                else
                {
                    img.Source = CreateErrorPlaceholder("Failed to load image");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image from {Source}", source);
                if (img != null)
                {
                    img.Source = CreateErrorPlaceholder($"Error: {ex.Message}");
                }
            }
        }

        private IImage CreateErrorPlaceholder(string message)
        {
            // 创建一个简单的错误占位图
            var drawingGroup = new DrawingGroup();
            using (var context = drawingGroup.Open())
            {
                context.DrawRectangle(
                    Brushes.LightGray,
                    new Pen(Brushes.Gray, 1),
                    new Rect(0, 0, 100, 100)
                );

                var text = new FormattedText(
                    message,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default),
                    12,
                    Brushes.Gray
                );

                context.DrawText(text, new Point(5, 40));
            }

            return new DrawingImage(drawingGroup);
        }

        private void OnLinkClicked(string url)
        {
            LinkClicked?.Invoke(this, url);
        }

        private Control RenderTable(TableElement table)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };

            // 添加列定义
            for (int i = 0; i < table.Headers.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // 添加行定义
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 表头行
            foreach (var row in table.Rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // 渲染表头
            for (int col = 0; col < table.Headers.Count; col++)
            {
                var headerCell = new Border
                {
                    Child = new TextBlock
                    {
                        Text = table.Headers[col],
                        FontWeight = FontWeight.Bold,
                        Padding = new Thickness(5),
                        TextWrapping = TextWrapping.Wrap
                    },
                    BorderBrush = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(247, 247, 247))
                };
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, col);
                grid.Children.Add(headerCell);
            }

            // 渲染数据行
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var rowData = table.Rows[row];
                for (int col = 0; col < Math.Min(rowData.Count, table.Headers.Count); col++)
                {
                    var cell = new Border
                    {
                        Child = new TextBlock
                        {
                            Text = rowData[col],
                            Padding = new Thickness(5),
                            TextWrapping = TextWrapping.Wrap
                        },
                        BorderBrush = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                        BorderThickness = new Thickness(1)
                    };
                    Grid.SetRow(cell, row + 1);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }

            return grid;
        }

        private void UpdateTable(Grid grid, TableElement table)
        {
            if (grid.Children == null)
                return;

            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            // Add column definitions
            foreach (var _ in table.Headers)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // Add header row
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < table.Headers.Count; i++)
            {
                var header = table.Headers[i];
                if (header == null)
                    continue;

                var headerCell = new TextBlock
                {
                    Text = header ?? string.Empty,
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, i);
                grid.Children.Add(headerCell);
            }

            // Add data rows
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                if (row == null)
                    continue;

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    var cell = row[colIndex];
                    var textBlock = new TextBlock
                    {
                        Text = cell ?? string.Empty,
                        Padding = new Thickness(5)
                    };
                    Grid.SetRow(textBlock, rowIndex + 1);
                    Grid.SetColumn(textBlock, colIndex);
                    grid.Children.Add(textBlock);
                }
            }
        }

        private Control RenderEmphasis(EmphasisElement emphasis)
        {
            var textBlock = new TextBlock();
            if (textBlock.Inlines != null)
            {
                if (emphasis.IsStrong)
                {
                    var bold = new Bold();
                    bold.Inlines?.Add(new Run { Text = emphasis.Text ?? string.Empty });
                    textBlock.Inlines.Add(bold);
                }
                else
                {
                    var italic = new Italic();
                    italic.Inlines?.Add(new Run { Text = emphasis.Text ?? string.Empty });
                    textBlock.Inlines.Add(italic);
                }
            }
            return textBlock;
        }

        private void UpdateEmphasis(TextBlock textBlock, EmphasisElement emphasis)
        {
            if (textBlock.Inlines == null)
                return;

            textBlock.Inlines.Clear();
            if (emphasis.IsStrong)
            {
                var bold = new Bold();
                bold.Inlines?.Add(new Run { Text = emphasis.Text ?? string.Empty });
                textBlock.Inlines.Add(bold);
            }
            else
            {
                var italic = new Italic();
                italic.Inlines?.Add(new Run { Text = emphasis.Text ?? string.Empty });
                textBlock.Inlines.Add(italic);
            }
        }
    }
}
