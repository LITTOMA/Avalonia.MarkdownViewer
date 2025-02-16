using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.TaskLists;
using MarkdownViewer.Core.Elements;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using System;
using Markdig.Parsers;

namespace MarkdownViewer.Core.Implementations
{
    public class MarkdigParser : IMarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdigParser()
        {
            var settings = new MarkdownPipelineBuilder();
            settings.UseAdvancedExtensions();
            settings.EnableTrackTrivia();
            settings.UsePreciseSourceLocation();
            settings.UseTaskLists();
            _pipeline = settings.Build();
        }

        public async IAsyncEnumerable<MarkdownElement> ParseStreamAsync(
            Stream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var reader = new StreamReader(stream);
            var currentBlock = new StringBuilder();
            string? line;
            bool isInCodeBlock = false;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                // 检查是否进入或离开代码块
                if (line.StartsWith("```"))
                {
                    isInCodeBlock = !isInCodeBlock;
                }

                // 只有在不在代码块内且遇到空行时才处理当前块
                if (!isInCodeBlock && string.IsNullOrWhiteSpace(line) && currentBlock.Length > 0)
                {
                    foreach (var element in ParseBlock(currentBlock.ToString()))
                    {
                        yield return element;
                    }
                    currentBlock.Clear();
                }

                currentBlock.AppendLine(line);
            }

            if (currentBlock.Length > 0)
            {
                foreach (var element in ParseBlock(currentBlock.ToString()))
                {
                    yield return element;
                }
            }
        }

        public async IAsyncEnumerable<MarkdownElement> ParseTextAsync(
            string text,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            await foreach (var element in ParseStreamAsync(stream, cancellationToken))
            {
                yield return element;
            }
        }

        private IEnumerable<MarkdownElement> ParseBlock(string blockText)
        {
            var document = Markdown.Parse(blockText, _pipeline);

            foreach (var block in document)
            {
                if (block is HeadingBlock heading)
                {
                    var element = new HeadingElement
                    {
                        RawText = blockText,
                        ElementType = Elements.MarkdownElementType.Heading,
                        Level = heading.Level,
                        Text = ProcessInlineElements(heading.Inline) ?? string.Empty
                    };
                    yield return element;
                }
                else if (block is ListBlock listBlock)
                {
                    if (
                        listBlock.IsOrdered == false
                        && listBlock.Any(x =>
                        {
                            if (x is ListItemBlock item)
                            {
                                var paragraph = item.Descendants<ParagraphBlock>().FirstOrDefault();
                                if (paragraph?.Inline?.FirstChild is LiteralInline literal)
                                {
                                    var content = literal.Content.ToString();
                                    return content.StartsWith("[ ] ")
                                        || content.StartsWith("[x] ")
                                        || content.StartsWith("[X] ");
                                }
                            }
                            return false;
                        })
                    )
                    {
                        var element = new TaskListElement
                        {
                            RawText = blockText,
                            ElementType = Elements.MarkdownElementType.TaskList,
                            Items = ParseTaskListItems(listBlock, 0)
                        };
                        yield return element;
                    }
                    else
                    {
                        var element = new ListElement
                        {
                            RawText = blockText,
                            ElementType = Elements.MarkdownElementType.List,
                            IsOrdered = listBlock.IsOrdered,
                            Items = ParseListItems(listBlock, 0)
                        };
                        yield return element;
                    }
                }
                else if (block is ParagraphBlock paragraph)
                {
                    if (paragraph.Inline?.FirstChild is LinkInline { IsImage: true } imageLink)
                    {
                        // 如果段落只包含一个图片，直接返回 ImageElement
                        yield return new ImageElement
                        {
                            RawText = blockText,
                            ElementType = Elements.MarkdownElementType.Image,
                            Source = imageLink.Url ?? string.Empty,
                            Title = imageLink.Title ?? string.Empty,
                            Alt = imageLink.Label?.ToString() ?? string.Empty
                        };
                    }
                    else
                    {
                        var element = new ParagraphElement
                        {
                            RawText = blockText,
                            ElementType = Elements.MarkdownElementType.Paragraph,
                            Text = string.Empty,
                            Inlines = new List<MarkdownElement>()
                        };

                        if (paragraph.Inline != null)
                        {
                            foreach (var inline in paragraph.Inline)
                            {
                                if (inline is LinkInline link)
                                {
                                    if (link.IsImage)
                                    {
                                        element.Inlines.Add(
                                            new ImageElement
                                            {
                                                RawText = link.ToString() ?? string.Empty,
                                                ElementType = Elements.MarkdownElementType.Image,
                                                Source = link.Url ?? string.Empty,
                                                Title = link.Title ?? string.Empty,
                                                Alt = link.Label?.ToString() ?? string.Empty
                                            }
                                        );
                                    }
                                    else
                                    {
                                        element.Inlines.Add(
                                            new LinkElement
                                            {
                                                RawText = link.ToString() ?? string.Empty,
                                                ElementType = Elements.MarkdownElementType.Link,
                                                Text = ProcessInlineElements(link),
                                                Url = link.Url ?? string.Empty,
                                                Title = link.Title ?? string.Empty
                                            }
                                        );
                                    }
                                }
                                else if (inline is EmphasisInline emphasis)
                                {
                                    element.Inlines.Add(
                                        new EmphasisElement
                                        {
                                            RawText = emphasis.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Emphasis,
                                            Text = ProcessInlineElements(emphasis),
                                            IsStrong = emphasis.DelimiterCount == 2
                                        }
                                    );
                                }
                                else if (inline is CodeInline code)
                                {
                                    element.Inlines.Add(
                                        new CodeInlineElement
                                        {
                                            RawText = code.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Code = code.Content.ToString()
                                        }
                                    );
                                }
                                else if (inline is LiteralInline literal)
                                {
                                    element.Inlines.Add(
                                        new TextElement
                                        {
                                            RawText = literal.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Text = literal.Content.ToString()
                                        }
                                    );
                                }
                                else
                                {
                                    element.Inlines.Add(
                                        new TextElement
                                        {
                                            RawText = inline.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Text = ProcessInline(inline)
                                        }
                                    );
                                }
                            }
                        }

                        yield return element;
                    }
                }
                else if (block is CodeBlock codeBlock)
                {
                    var fencedCodeBlock = (FencedCodeBlock)codeBlock;
                    var codeLines = fencedCodeBlock.Lines.Lines
                        .Take(fencedCodeBlock.Lines.Count)
                        .Select(x => x.ToString())
                        .ToList();

                    var element = new CodeBlockElement
                    {
                        RawText = blockText,
                        ElementType = Elements.MarkdownElementType.CodeBlock,
                        Code = string.Join(Environment.NewLine, codeLines),
                        Language = fencedCodeBlock.Info ?? string.Empty
                    };
                    yield return element;
                }
                else if (block is QuoteBlock quoteBlock)
                {
                    var element = new QuoteElement
                    {
                        RawText = blockText,
                        ElementType = Elements.MarkdownElementType.Quote,
                        Text = string.Empty,
                        Inlines = new List<MarkdownElement>()
                    };

                    foreach (var line in quoteBlock)
                    {
                        if (line is ParagraphBlock quoteParagraph)
                        {
                            if (quoteParagraph.Inline?.FirstChild is LinkInline { IsImage: true } imageLink)
                            {
                                element.Inlines.Add(new ImageElement
                                {
                                    RawText = imageLink.ToString() ?? string.Empty,
                                    ElementType = Elements.MarkdownElementType.Image,
                                    Source = imageLink.Url ?? string.Empty,
                                    Title = imageLink.Title ?? string.Empty,
                                    Alt = imageLink.Label?.ToString() ?? string.Empty
                                });
                            }
                            else if (quoteParagraph.Inline != null)
                            {
                                foreach (var inline in quoteParagraph.Inline)
                                {
                                    if (inline is LinkInline link)
                                    {
                                        if (link.IsImage)
                                        {
                                            element.Inlines.Add(new ImageElement
                                            {
                                                RawText = link.ToString() ?? string.Empty,
                                                ElementType = Elements.MarkdownElementType.Image,
                                                Source = link.Url ?? string.Empty,
                                                Title = link.Title ?? string.Empty,
                                                Alt = link.Label?.ToString() ?? string.Empty
                                            });
                                        }
                                        else
                                        {
                                            element.Inlines.Add(new LinkElement
                                            {
                                                RawText = link.ToString() ?? string.Empty,
                                                ElementType = Elements.MarkdownElementType.Link,
                                                Text = ProcessInlineElements(link),
                                                Url = link.Url ?? string.Empty,
                                                Title = link.Title ?? string.Empty
                                            });
                                        }
                                    }
                                    else if (inline is EmphasisInline emphasis)
                                    {
                                        element.Inlines.Add(new EmphasisElement
                                        {
                                            RawText = emphasis.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Emphasis,
                                            Text = ProcessInlineElements(emphasis),
                                            IsStrong = emphasis.DelimiterCount == 2
                                        });
                                    }
                                    else if (inline is CodeInline code)
                                    {
                                        element.Inlines.Add(new CodeInlineElement
                                        {
                                            RawText = code.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Code = code.Content.ToString()
                                        });
                                    }
                                    else if (inline is LiteralInline literal)
                                    {
                                        element.Inlines.Add(new TextElement
                                        {
                                            RawText = literal.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Text = literal.Content.ToString()
                                        });
                                    }
                                    else
                                    {
                                        element.Inlines.Add(new TextElement
                                        {
                                            RawText = inline.ToString() ?? string.Empty,
                                            ElementType = Elements.MarkdownElementType.Text,
                                            Text = ProcessInline(inline)
                                        });
                                    }
                                }
                            }
                        }
                    }
                    yield return element;
                }
                else if (block is ThematicBreakBlock)
                {
                    var element = new HorizontalRuleElement
                    {
                        RawText = blockText,
                        ElementType = Elements.MarkdownElementType.HorizontalRule
                    };
                    yield return element;
                }
                else if (block is Table table)
                {
                    var element = new TableElement
                    {
                        RawText = blockText,
                        ElementType = Elements.MarkdownElementType.Table,
                        Headers =
                            table.Count > 0 && table[0] is TableRow headerRow
                                ? headerRow.Select(cell => GetCellContent((TableCell)cell)).ToList()
                                : new List<string>(),
                        Rows = table
                            .Skip(1)
                            .Where(row => row is TableRow)
                            .Select(
                                row =>
                                    ((TableRow)row)
                                        .Select(cell => GetCellContent((TableCell)cell))
                                        .ToList()
                            )
                            .ToList()
                    };
                    yield return element;
                }
            }
        }

        private string GetCellContent(Block cell)
        {
            if (cell == null)
                return string.Empty;

            var paragraph = cell.Descendants<ParagraphBlock>().FirstOrDefault();
            if (paragraph?.Inline == null)
                return string.Empty;

            var content = new StringBuilder();
            foreach (var inline in paragraph.Inline)
            {
                if (inline is CodeInline codeInline)
                {
                    content.Append('`').Append(codeInline.Content).Append('`');
                }
                else if (inline is LinkInline link)
                {
                    if (link.IsImage)
                    {
                        content
                            .Append("![")
                            .Append(link.Label ?? string.Empty)
                            .Append("](")
                            .Append(link.Url)
                            .Append(link.Title != null ? $" \"{link.Title}\"" : string.Empty)
                            .Append(")");
                    }
                    else
                    {
                        content
                            .Append("[")
                            .Append(ProcessInlineElements(link))
                            .Append("](")
                            .Append(link.Url)
                            .Append(")");
                    }
                }
                else if (inline is LiteralInline literal)
                {
                    content.Append(literal.Content);
                }
                else
                {
                    content.Append(ProcessInline(inline));
                }
            }
            return content.ToString();
        }

        private string ProcessInlineElements(ContainerInline? container)
        {
            if (container == null)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var inline in container)
            {
                builder.Append(ProcessInline(inline));
            }
            return builder.ToString();
        }

        private string ProcessInline(Inline inline)
        {
            return inline switch
            {
                LiteralInline literal => literal.Content.ToString() ?? string.Empty,
                LinkInline link => link.Title ?? link.Url ?? string.Empty,
                EmphasisInline emphasis => ProcessInlineElements(emphasis),
                LineBreakInline => string.Empty,
                TaskList taskList => taskList.Checked ? "[x] " : "[ ] ",
                _ => inline.ToString() ?? string.Empty
            };
        }

        private List<ListItemElement> ParseListItems(ListBlock listBlock, int level)
        {
            var items = new List<ListItemElement>();
            foreach (var item in listBlock)
            {
                if (item is ListItemBlock listItem)
                {
                    var text = string.Empty;
                    var paragraph = listItem.Descendants<ParagraphBlock>().FirstOrDefault();
                    if (paragraph?.Inline != null)
                    {
                        text = ProcessInlineElements(paragraph.Inline);
                    }

                    var element = new ListItemElement
                    {
                        RawText = item.ToString() ?? string.Empty,
                        ElementType = Elements.MarkdownElementType.ListItem,
                        Text = text,
                        Level = level
                    };

                    // 处理子列表
                    var subList = listItem.Descendants<ListBlock>().FirstOrDefault();
                    if (subList != null)
                    {
                        element.Children = ParseListItems(subList, level + 1);
                    }

                    items.Add(element);
                }
            }
            return items;
        }

        private List<TaskListItemElement> ParseTaskListItems(ListBlock listBlock, int level)
        {
            var items = new List<TaskListItemElement>();
            foreach (var item in listBlock)
            {
                if (item is ListItemBlock listItem)
                {
                    var text = string.Empty;
                    var isChecked = false;

                    var paragraph = listItem.Descendants<ParagraphBlock>().FirstOrDefault();
                    if (paragraph?.Inline != null)
                    {
                        // 处理所有内联元素
                        text = ProcessInlineElements(paragraph.Inline);

                        // 检查是否包含任务列表标记并提取
                        if (
                            text.StartsWith("[ ] ")
                            || text.StartsWith("[x] ")
                            || text.StartsWith("[X] ")
                        )
                        {
                            isChecked = text.StartsWith("[x] ") || text.StartsWith("[X] ");
                            text = text.Substring(4); // 跳过"[ ] "或"[x] "
                        }
                    }

                    var element = new TaskListItemElement
                    {
                        RawText = item.ToString() ?? string.Empty,
                        ElementType = Elements.MarkdownElementType.TaskListItem,
                        Text = text,
                        IsChecked = isChecked,
                        Level = level
                    };

                    // 处理子列表
                    var childList = listItem.Descendants<ListBlock>().FirstOrDefault();
                    if (childList != null)
                    {
                        element.Children = ParseTaskListItems(childList, level + 1);
                    }

                    items.Add(element);
                }
            }
            return items;
        }
    }
}
