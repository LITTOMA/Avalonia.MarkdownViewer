using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using System.Globalization;
using Microsoft.Extensions.Logging;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Services;
using AvaloniaMath.Controls;
using System.Collections.Generic;
using System.Linq;

namespace MarkdownViewer.Core.Implementations
{
    public class AvaloniaMarkdownRenderer : IMarkdownRenderer
    {
        private static readonly FontFamily CodeFontFamily =
            new("Consolas, Menlo, Monaco, monospace");

        private readonly FontFamily _defaultFontFamily = FontFamily.Default;
        private readonly double _baseFontSize = 14;
        private readonly IImageCache _imageCache;
        private readonly ILogger _logger;

        // 差分更新相关字段
        private string? _cachedMarkdown;
        private List<MarkdownElement>? _cachedElements;
        private Control? _cachedControl;
        private StackPanel? _cachedPanel;

        public event EventHandler<string>? LinkClicked;

        public AvaloniaMarkdownRenderer(
            IImageCache imageCache,
            ILogger<AvaloniaMarkdownRenderer> logger
        )
        {
            _imageCache = imageCache;
            _logger = logger;

            // Initialize theme resources
            MarkdownTheme.Initialize();
        }

        // Get theme-related colors (kept for potential future use)
        private IBrush GetThemeBrush(string resourceKey, Color fallbackColor)
        {
            return MarkdownTheme.GetThemeBrush(resourceKey, fallbackColor);
        }

        private void RenderInlineElements(TextBlock textBlock, List<MarkdownElement> inlines)
        {
            if (inlines == null || inlines.Count == 0)
                return;

            foreach (var inline in inlines)
            {
                if (inline == null)
                    continue;

                switch (inline)
                {
                    case Elements.TextElement text:
                        textBlock.Inlines?.Add(
                            new Run
                            {
                                Text = text.Text ?? string.Empty,
                                BaselineAlignment = BaselineAlignment.Center
                            }
                        );
                        break;
                    case EmphasisElement emphasis:
                        RenderEmphasisInline(textBlock, emphasis);
                        break;
                    case CodeInlineElement code:
                        textBlock.Inlines?.Add(
                            new InlineUIContainer
                            {
                                Child = CreateCodeBorder(code.Code ?? string.Empty)
                            }
                        );
                        break;
                    case LinkElement link:
                        textBlock.Inlines?.Add(
                            new InlineUIContainer
                            {
                                Child = CreateLinkButton(link.Text ?? string.Empty, link.Url)
                            }
                        );
                        break;
                    case ImageElement image:
                        var img = new Image
                        {
                            Stretch = Stretch.Uniform,
                            StretchDirection = StretchDirection.DownOnly,
                            MaxHeight = 400,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        LoadImageAsync(img, image.Source);
                        textBlock.Inlines?.Add(new InlineUIContainer { Child = img });
                        break;
                    case MathInlineElement mathInline:
                        textBlock.Inlines?.Add(
                            new InlineUIContainer { Child = RenderMathInline(mathInline) }
                        );
                        break;
                }
            }
        }

        private void RenderEmphasisInline(TextBlock textBlock, EmphasisElement emphasis)
        {
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
        }

        private TextBlock CreateListItemContent(ListItemElement item)
        {
            var content = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Process inline elements if available
            if (item.Inlines != null && item.Inlines.Count > 0)
            {
                RenderInlineElements(content, item.Inlines);
            }
            else
            {
                // Fallback to plain text
                content.Text = item.Text ?? string.Empty;
            }

            return content;
        }

        private TextBlock CreateTaskListItemContent(TaskListItemElement item)
        {
            var content = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Process inline elements if available
            if (item.Inlines != null && item.Inlines.Count > 0)
            {
                RenderInlineElements(content, item.Inlines);
            }
            else
            {
                // Fallback to plain text
                content.Text = item.Text ?? string.Empty;
            }

            return content;
        }

        public Control RenderDocument(string markdown)
        {
            // 检查是否有缓存且内容相同
            if (_cachedMarkdown == markdown && _cachedControl != null)
            {
                return _cachedControl;
            }

            var parser = new MarkdigParser();
            var elements = parser.ParseTextAsync(markdown).ToBlockingEnumerable().ToList();

            // 如果有缓存，尝试差分更新
            if (_cachedElements != null && _cachedPanel != null)
            {
                var updatedControl = TryDifferentialUpdate(markdown, elements);
                if (updatedControl != null)
                {
                    return updatedControl;
                }
            }

            // 完整重新渲染
            return RenderDocumentFull(markdown, elements);
        }

        private Control TryDifferentialUpdate(string markdown, List<MarkdownElement> newElements)
        {
            try
            {
                if (_cachedElements == null || _cachedPanel == null)
                    return null;

                var differences = FindElementDifferences(_cachedElements, newElements);

                if (differences.Count == 0)
                {
                    // 没有差异，返回缓存的控制
                    return _cachedControl!;
                }

                // 应用差分更新
                ApplyDifferentialUpdates(differences, newElements);

                // 更新缓存
                _cachedMarkdown = markdown;
                _cachedElements = newElements;

                return _cachedControl!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Differential update failed, falling back to full render");
                return null;
            }
        }

        private Control RenderDocumentFull(string markdown, List<MarkdownElement> elements)
        {
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

            // 更新缓存
            _cachedMarkdown = markdown;
            _cachedElements = elements;
            _cachedControl = panel;
            _cachedPanel = panel;

            return panel;
        }

        private List<ElementDifference> FindElementDifferences(
            List<MarkdownElement> oldElements,
            List<MarkdownElement> newElements
        )
        {
            var differences = new List<ElementDifference>();

            // 使用最长公共子序列算法找到差异
            var lcs = ComputeLongestCommonSubsequence(oldElements, newElements);

            int oldIndex = 0,
                newIndex = 0,
                lcsIndex = 0;

            while (oldIndex < oldElements.Count || newIndex < newElements.Count)
            {
                if (
                    oldIndex < oldElements.Count
                    && newIndex < newElements.Count
                    && lcsIndex < lcs.Count
                    && AreElementsEqual(oldElements[oldIndex], newElements[newIndex])
                )
                {
                    // 元素相同，跳过
                    oldIndex++;
                    newIndex++;
                    lcsIndex++;
                }
                else if (
                    oldIndex < oldElements.Count
                    && (
                        lcsIndex >= lcs.Count
                        || !AreElementsEqual(oldElements[oldIndex], lcs[lcsIndex])
                    )
                )
                {
                    // 旧元素被删除
                    differences.Add(
                        new ElementDifference
                        {
                            Type = DifferenceType.Removed,
                            OldIndex = oldIndex,
                            Element = oldElements[oldIndex]
                        }
                    );
                    oldIndex++;
                }
                else if (
                    newIndex < newElements.Count
                    && (
                        lcsIndex >= lcs.Count
                        || !AreElementsEqual(newElements[newIndex], lcs[lcsIndex])
                    )
                )
                {
                    // 新元素被添加
                    differences.Add(
                        new ElementDifference
                        {
                            Type = DifferenceType.Added,
                            NewIndex = newIndex,
                            Element = newElements[newIndex]
                        }
                    );
                    newIndex++;
                }
                else
                {
                    oldIndex++;
                    newIndex++;
                }
            }

            return differences;
        }

        private List<MarkdownElement> ComputeLongestCommonSubsequence(
            List<MarkdownElement> oldElements,
            List<MarkdownElement> newElements
        )
        {
            var lcs = new List<MarkdownElement>();

            for (int i = 0; i < oldElements.Count; i++)
            {
                for (int j = 0; j < newElements.Count; j++)
                {
                    if (AreElementsEqual(oldElements[i], newElements[j]))
                    {
                        lcs.Add(oldElements[i]);
                        break;
                    }
                }
            }

            return lcs;
        }

        private bool AreElementsEqual(MarkdownElement? element1, MarkdownElement? element2)
        {
            if (element1 == null || element2 == null)
                return element1 == element2;

            if (element1.ElementType != element2.ElementType)
                return false;

            return element1 switch
            {
                HeadingElement h1 when element2 is HeadingElement h2
                    => h1.Level == h2.Level && h1.Text == h2.Text,

                ParagraphElement p1 when element2 is ParagraphElement p2
                    => p1.Text == p2.Text && AreInlineElementsEqual(p1.Inlines, p2.Inlines),

                CodeBlockElement c1 when element2 is CodeBlockElement c2
                    => c1.Code == c2.Code && c1.Language == c2.Language,

                ImageElement img1 when element2 is ImageElement img2
                    => img1.Source == img2.Source && img1.Alt == img2.Alt,

                LinkElement l1 when element2 is LinkElement l2
                    => l1.Url == l2.Url && l1.Text == l2.Text,

                ListElement list1 when element2 is ListElement list2
                    => list1.IsOrdered == list2.IsOrdered
                        && AreListItemsEqual(list1.Items, list2.Items),

                TaskListElement task1 when element2 is TaskListElement task2
                    => AreTaskListItemsEqual(task1.Items, task2.Items),

                QuoteElement q1 when element2 is QuoteElement q2
                    => q1.Text == q2.Text && AreInlineElementsEqual(q1.Inlines, q2.Inlines),

                TableElement t1 when element2 is TableElement t2 => AreTableElementsEqual(t1, t2),

                EmphasisElement e1 when element2 is EmphasisElement e2
                    => e1.Text == e2.Text && e1.IsStrong == e2.IsStrong,

                CodeInlineElement ci1 when element2 is CodeInlineElement ci2
                    => ci1.Code == ci2.Code,

                MathBlockElement mb1 when element2 is MathBlockElement mb2
                    => mb1.Content == mb2.Content,

                MathInlineElement mi1 when element2 is MathInlineElement mi2
                    => mi1.Content == mi2.Content,

                Elements.TextElement te1 when element2 is Elements.TextElement te2
                    => te1.Text == te2.Text,

                HorizontalRuleElement when element2 is HorizontalRuleElement => true,

                _ => false
            };
        }

        private bool AreInlineElementsEqual(
            List<MarkdownElement>? inlines1,
            List<MarkdownElement>? inlines2
        )
        {
            if (inlines1 == null || inlines2 == null)
                return inlines1 == inlines2;

            if (inlines1.Count != inlines2.Count)
                return false;

            for (int i = 0; i < inlines1.Count; i++)
            {
                if (!AreElementsEqual(inlines1[i], inlines2[i]))
                    return false;
            }

            return true;
        }

        private bool AreListItemsEqual(List<ListItemElement>? items1, List<ListItemElement>? items2)
        {
            if (items1 == null || items2 == null)
                return items1 == items2;

            if (items1.Count != items2.Count)
                return false;

            for (int i = 0; i < items1.Count; i++)
            {
                var item1 = items1[i];
                var item2 = items2[i];

                if (
                    item1.Text != item2.Text
                    || item1.Level != item2.Level
                    || !AreListItemsEqual(item1.Children, item2.Children)
                    || !AreInlineElementsEqual(item1.Inlines, item2.Inlines)
                )
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreTaskListItemsEqual(
            List<TaskListItemElement>? items1,
            List<TaskListItemElement>? items2
        )
        {
            if (items1 == null || items2 == null)
                return items1 == items2;

            if (items1.Count != items2.Count)
                return false;

            for (int i = 0; i < items1.Count; i++)
            {
                var item1 = items1[i];
                var item2 = items2[i];

                if (
                    item1.Text != item2.Text
                    || item1.IsChecked != item2.IsChecked
                    || item1.Level != item2.Level
                    || !AreTaskListItemsEqual(item1.Children, item2.Children)
                    || !AreInlineElementsEqual(item1.Inlines, item2.Inlines)
                )
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreTableElementsEqual(TableElement table1, TableElement table2)
        {
            if (table1.Headers.Count != table2.Headers.Count)
                return false;

            for (int i = 0; i < table1.Headers.Count; i++)
            {
                if (table1.Headers[i] != table2.Headers[i])
                    return false;
            }

            if (table1.Rows.Count != table2.Rows.Count)
                return false;

            for (int i = 0; i < table1.Rows.Count; i++)
            {
                var row1 = table1.Rows[i];
                var row2 = table2.Rows[i];

                if (row1.Count != row2.Count)
                    return false;

                for (int j = 0; j < row1.Count; j++)
                {
                    if (row1[j] != row2[j])
                        return false;
                }
            }

            return true;
        }

        private void ApplyDifferentialUpdates(
            List<ElementDifference> differences,
            List<MarkdownElement> newElements
        )
        {
            if (_cachedPanel == null)
                return;

            // 按索引倒序处理删除操作，避免索引变化
            var removals = differences
                .Where(d => d.Type == DifferenceType.Removed)
                .OrderByDescending(d => d.OldIndex)
                .ToList();

            foreach (var removal in removals)
            {
                if (removal.OldIndex < _cachedPanel.Children.Count)
                {
                    _cachedPanel.Children.RemoveAt(removal.OldIndex);
                }
            }

            // 按索引正序处理添加操作
            var additions = differences
                .Where(d => d.Type == DifferenceType.Added)
                .OrderBy(d => d.NewIndex)
                .ToList();

            foreach (var addition in additions)
            {
                var control = RenderElement(addition.Element);
                var insertIndex = Math.Min(addition.NewIndex, _cachedPanel.Children.Count);
                _cachedPanel.Children.Insert(insertIndex, control);
            }
        }

        // 差分更新相关的辅助类
        private class ElementDifference
        {
            public DifferenceType Type { get; set; }
            public int OldIndex { get; set; } = -1;
            public int NewIndex { get; set; } = -1;
            public MarkdownElement Element { get; set; } = null!;
        }

        private enum DifferenceType
        {
            Added,
            Removed
        }

        private Control RenderElement(MarkdownElement element)
        {
            return element switch
            {
                HeadingElement heading => RenderHeading(heading),
                ParagraphElement paragraph => RenderParagraph(paragraph),
                CodeBlockElement codeBlock => RenderCodeBlock(codeBlock),
                ListElement list => RenderList(list),
                TaskListElement taskList => RenderTaskList(taskList),
                QuoteElement quote => RenderQuote(quote),
                ImageElement image => RenderImage(image),
                LinkElement link => RenderLink(link),
                TableElement table => RenderTable(table),
                EmphasisElement emphasis => RenderEmphasis(emphasis),
                HorizontalRuleElement => RenderHorizontalRule(),
                MathBlockElement mathBlock => RenderMathBlock(mathBlock),
                MathInlineElement mathInline => RenderMathInline(mathInline),
                _ => new TextBlock { Text = "Unsupported element" }
            };
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
            // If paragraph contains only one image element, return image control directly
            if (paragraph.Inlines.Count == 1 && paragraph.Inlines[0] is ImageElement image)
            {
                return RenderImage(image);
            }

            var textBlock = new TextBlock
            {
                FontFamily = _defaultFontFamily,
                FontSize = _baseFontSize,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            if (paragraph.Inlines != null)
            {
                RenderInlineElements(textBlock, paragraph.Inlines);
            }

            return textBlock;
        }

        private Tuple<string, string> GetCopyButtonText()
        {
            // Get current system language code
            var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

            return currentCulture switch
            {
                "zh" => Tuple.Create("复制", "已复制"),
                "ja" => Tuple.Create("コピー", "コピーしました"),
                "ko" => Tuple.Create("복사", "복사됨"),
                "fr" => Tuple.Create("Copier", "Copié"),
                "de" => Tuple.Create("Kopieren", "Kopiert"),
                "es" => Tuple.Create("Copiar", "Copiado"),
                "it" => Tuple.Create("Copia", "Copiato"),
                "ru" => Tuple.Create("Копировать", "Скопировано"),
                "pt" => Tuple.Create("Copiar", "Copiado"),
                "nl" => Tuple.Create("Kopiëren", "Gekopieerd"),
                "pl" => Tuple.Create("Kopiuj", "Skopiowano"),
                "tr" => Tuple.Create("Kopyala", "Kopyalandı"),
                "ar" => Tuple.Create("نسخ", "تم النسخ"),
                "hi" => Tuple.Create("कॉपी", "कॉपी किया गया"),
                "th" => Tuple.Create("คัดลอก", "คัดลอกแล้ว"),
                "vi" => Tuple.Create("Sao chép", "Đã sao chép"),
                "cs" => Tuple.Create("Kopírovat", "Zkopírováno"),
                "sv" => Tuple.Create("Kopiera", "Kopierat"),
                "el" => Tuple.Create("Αντιγραφή", "Αντιγράφηκε"),
                "he" => Tuple.Create("העתק", "הועתק"),
                "hu" => Tuple.Create("Másolás", "Másolva"),
                "ro" => Tuple.Create("Copiază", "Copiat"),
                "uk" => Tuple.Create("Копіювати", "Скопійовано"),
                "fi" => Tuple.Create("Kopioi", "Kopioitu"),
                "da" => Tuple.Create("Kopiér", "Kopieret"),
                "id" => Tuple.Create("Salin", "Disalin"),
                "ms" => Tuple.Create("Salin", "Disalin"),
                "bn" => Tuple.Create("কপি", "কপি করা হয়েছে"),
                "fa" => Tuple.Create("کپی", "کپی شد"),
                "bg" => Tuple.Create("Копирай", "Копирано"),
                "sk" => Tuple.Create("Kopírovať", "Skopírované"),
                "hr" => Tuple.Create("Kopiraj", "Kopirano"),
                "sr" => Tuple.Create("Копирај", "Копирано"),
                "sl" => Tuple.Create("Kopiraj", "Kopirano"),
                "et" => Tuple.Create("Kopeeri", "Kopeeritud"),
                "lv" => Tuple.Create("Kopēt", "Nokopēts"),
                "lt" => Tuple.Create("Kopijuoti", "Nukopijuota"),
                "no" => Tuple.Create("Kopier", "Kopiert"),
                _ => Tuple.Create("Copy", "Copied") // 默认英文
            };
        }

        private Control RenderCodeBlock(CodeBlockElement codeBlock)
        {
            var grid = new Grid();

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 10)
            };
            border.Classes.Add("markdownCodeBlock");

            var textBox = new TextBlock
            {
                Text = codeBlock.Code,
                FontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace"),
                FontSize = _baseFontSize,
                Padding = new Thickness(16, 12, 16, 12),
                TextWrapping = TextWrapping.Wrap
            };

            var (copyText, copiedText) = GetCopyButtonText();
            var copyButton = new Button
            {
                Content = copyText,
                Margin = new Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                IsVisible = false,
                Padding = new Thickness(8, 4, 8, 4),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                Tag = false, // Copy button state
            };
            copyButton.Classes.Add("markdownCopyButton");

            copyButton.Click += async (s, e) =>
            {
                if (copyButton.Tag is true)
                    return;

                var topLevel = TopLevel.GetTopLevel(copyButton);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(codeBlock.Code);
                    copyButton.Content = copiedText;
                    copyButton.Tag = true;
                    await Task.Delay(2000);
                    copyButton.Content = copyText;
                    copyButton.Tag = false;
                }
            };

            border.Child = textBox;
            grid.Children.Add(border);
            Grid.SetColumn(border, 0);

            grid.Children.Add(copyButton);
            Grid.SetColumn(copyButton, 0);

            grid.PointerEntered += (s, e) => copyButton.IsVisible = true;
            grid.PointerExited += (s, e) => copyButton.IsVisible = false;

            return grid;
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
                        Margin = new Thickness(item.Level * 20, 0, 0, 0),
                        Spacing = 5
                    };

                    // Select different symbols based on level and list type
                    string bulletText = list.IsOrdered
                        ? $"{list.Items.IndexOf(item) + 1}."
                        : (item.Level == 0 ? "•" : "◦");

                    var bullet = new TextBlock
                    {
                        Text = bulletText,
                        Width = 20,
                        TextAlignment = TextAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    var contentPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 5
                    };

                    // Create content using helper method
                    var content = CreateListItemContent(item);
                    contentPanel.Children?.Add(content);

                    // Process sub-items
                    if (item.Children != null && item.Children.Count > 0)
                    {
                        var subList = new ListElement
                        {
                            RawText = string.Empty,
                            Items = item.Children,
                            IsOrdered = list.IsOrdered
                        };
                        var subListControl = RenderList(subList);
                        contentPanel.Children?.Add(subListControl);
                    }

                    itemPanel.Children?.Add(bullet);
                    itemPanel.Children?.Add(contentPanel);
                    panel.Children?.Add(itemPanel);
                }
            }

            return panel;
        }

        private Control RenderTaskList(TaskListElement taskList)
        {
            var panel = new StackPanel { Spacing = 5, Margin = new Thickness(0, 0, 0, 10) };

            if (taskList.Items != null)
            {
                foreach (var item in taskList.Items)
                {
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(item.Level * 20, 0, 0, 0),
                        Spacing = 5
                    };

                    var checkbox = new CheckBox
                    {
                        IsChecked = item.IsChecked,
                        IsEnabled = false, // Set to read-only
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    var contentPanel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 5
                    };

                    // Create content using helper method
                    var content = CreateTaskListItemContent(item);
                    contentPanel.Children?.Add(content);

                    // Process sub-items
                    if (item.Children != null && item.Children.Count > 0)
                    {
                        var subList = new TaskListElement
                        {
                            RawText = string.Empty,
                            Items = item.Children
                        };
                        var subListControl = RenderTaskList(subList);
                        contentPanel.Children?.Add(subListControl);
                    }

                    itemPanel.Children?.Add(checkbox);
                    itemPanel.Children?.Add(contentPanel);
                    panel.Children?.Add(itemPanel);
                }
            }

            return panel;
        }

        private Control RenderQuote(QuoteElement quote)
        {
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };
            textBlock.Classes.Add("markdownQuoteText");

            if (quote.Inlines != null)
            {
                RenderInlineElements(textBlock, quote.Inlines);
            }

            var border = new Border
            {
                Child = textBlock,
                BorderThickness = new Thickness(4, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };
            border.Classes.Add("markdownQuote");

            return border;
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
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextDecorations = TextDecorations.Underline,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            textBlock.Classes.Add("markdownLink");

            textBlock.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    DefaultLinkHandler.HandleLink(link.Url);
                    LinkClicked?.Invoke(this, link.Url);
                }
            };

            textBlock.Text = link.Text;
            return textBlock;
        }

        private async void LoadImageAsync(Image img, string source)
        {
            if (string.IsNullOrEmpty(source) || img == null)
                return;

            try
            {
                var imageData = await _imageCache.GetImageAsync(source);
                if (imageData != null)
                {
                    using var stream = new MemoryStream(imageData);
                    var bitmap = new Bitmap(stream);

                    // Set image source on UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        img.Source = bitmap;
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to load image from {Source}", source);
                    img.Source = CreateErrorPlaceholder("Failed to load image");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading image from {Source}", source);
                img.Source = CreateErrorPlaceholder($"Error: {ex.Message}");
            }
        }

        private IImage CreateErrorPlaceholder(string message)
        {
            // Create a simple error placeholder
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

        private Control RenderTable(TableElement table)
        {
            // Create an outer border container with rounded corners
            var outerBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 10),
                ClipToBounds = true // Ensure content doesn't exceed rounded border
            };
            outerBorder.Classes.Add("markdownTable");

            var grid = new Grid();

            // Add column definitions
            for (int i = 0; i < table.Headers.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // Add row definitions
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header row
            foreach (var _ in table.Rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Render headers
            for (int col = 0; col < table.Headers.Count; col++)
            {
                var headerCell = CreateTableCell(table.Headers[col], true);
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, col);
                grid.Children.Add(headerCell);
            }

            // Render data rows
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var dataRow = table.Rows[row];
                for (int col = 0; col < dataRow.Count; col++)
                {
                    var cell = CreateTableCell(dataRow[col]);
                    Grid.SetRow(cell, row + 1);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }

            outerBorder.Child = grid;
            return outerBorder;
        }

        private Border CreateTableCell(string content, bool isHeader = false)
        {
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(5)
            };

            // Handle image markup
            if (content.StartsWith("![") && content.Contains("]("))
            {
                var altEnd = content.IndexOf("]");
                var urlStart = content.IndexOf("(", altEnd);
                var urlEnd = content.IndexOf(")", urlStart);

                if (altEnd >= 0 && urlStart >= 0 && urlEnd >= 0)
                {
                    var alt = content.Substring(2, altEnd - 2);
                    var url = content.Substring(urlStart + 1, urlEnd - urlStart - 1);

                    var img = new Image
                    {
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.DownOnly,
                        MaxHeight = 100, // Use smaller image height in table
                        Margin = new Thickness(0)
                    };
                    LoadImageAsync(img, url);

                    var border = new Border
                    {
                        Child = img,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(2)
                    };
                    border.Classes.Add(isHeader ? "markdownTableHeader" : "markdownTableCell");
                    return border;
                }
            }
            // Handle link markup
            else if (content.StartsWith("[") && content.Contains("]("))
            {
                var textEnd = content.IndexOf("]");
                var urlStart = content.IndexOf("(", textEnd);
                var urlEnd = content.IndexOf(")", urlStart);

                if (textEnd >= 0 && urlStart >= 0 && urlEnd >= 0)
                {
                    var linkText = content.Substring(1, textEnd - 1);
                    var url = content.Substring(urlStart + 1, urlEnd - urlStart - 1);

                    var button = CreateLinkButton(linkText, url);
                    var border = new Border
                    {
                        Child = button,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(2)
                    };
                    border.Classes.Add(isHeader ? "markdownTableHeader" : "markdownTableCell");
                    return border;
                }
            }
            // Handle code markup
            else if (content.StartsWith("`") && content.EndsWith("`"))
            {
                var code = content.Trim('`');
                var codeBorder = CreateCodeBorder(code);
                var border = new Border
                {
                    Child = codeBorder,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(2)
                };
                border.Classes.Add(isHeader ? "markdownTableHeader" : "markdownTableCell");
                return border;
            }

            // Plain text
            textBlock.Text = content;
            if (isHeader)
            {
                textBlock.FontWeight = FontWeight.Bold;
            }

            var cellBorder = new Border { Child = textBlock, BorderThickness = new Thickness(1) };
            cellBorder.Classes.Add(isHeader ? "markdownTableHeader" : "markdownTableCell");
            return cellBorder;
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

        private Control RenderHorizontalRule()
        {
            var border = new Border { Height = 1, Margin = new Thickness(0, 10, 0, 10) };
            border.Classes.Add("markdownHorizontalRule");
            return border;
        }

        private Button CreateLinkButton(string text, string url)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                TextDecorations = TextDecorations.Underline
            };
            textBlock.Classes.Add("markdownLinkButtonText");

            var button = new Button
            {
                Content = textBlock,
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            button.Classes.Add("markdownLinkButton");

            button.Click += (s, e) =>
            {
                DefaultLinkHandler.HandleLink(url);
                LinkClicked?.Invoke(this, url);
            };

            return button;
        }

        private Border CreateCodeBorder(string code)
        {
            var codeText = new TextBlock
            {
                Text = code,
                FontFamily = CodeFontFamily,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                BaselineOffset = 1,
                FontSize = _baseFontSize * 0.9
            };

            var border = new Border
            {
                Child = codeText,
                Padding = new Thickness(6, 2, 6, 2),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, -1)
            };
            border.Classes.Add("markdownInlineCode");

            return border;
        }

        private Control RenderMathBlock(MathBlockElement mathBlock)
        {
            return new FormulaBlock
            {
                Formula = mathBlock.Content,
                FontSize = _baseFontSize * 1.2,
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }

        private Control RenderMathInline(MathInlineElement mathInline)
        {
            // AvaloniaMath 只提供 FormulaBlock，没有 FormulaInline，行内公式用 FormulaBlock 并缩小字号和去除上下边距
            return new FormulaBlock
            {
                Formula = mathInline.Content,
                FontSize = _baseFontSize,
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }
    }
}
