namespace MarkdownViewer.Core.Elements
{
    public enum MarkdownHeadingLevel
    {
        H1,
        H2,
        H3,
        H4,
        H5
    }

    public abstract class MarkdownElement
    {
        public required string RawText { get; set; }
    }

    public class TextElement : MarkdownElement
    {
        public required string Text { get; set; }
    }

    public class HeadingElement : MarkdownElement
    {
        public MarkdownHeadingLevel Level { get; set; }
        public required string Text { get; set; }
    }

    public class ParagraphElement : MarkdownElement
    {
        public required string Text { get; set; }
        public List<MarkdownElement> Inlines { get; set; } = new();
    }

    public class CodeBlockElement : MarkdownElement
    {
        public required string Code { get; set; }
        public required string Language { get; set; }
    }

    public class ImageElement : MarkdownElement
    {
        public required string Source { get; set; }
        public string? Alt { get; set; }
        public string? Title { get; set; }
    }

    public class LinkElement : MarkdownElement
    {
        public required string Url { get; set; }
        public required string Text { get; set; }
        public string? Title { get; set; }
    }

    public class ListElement : MarkdownElement
    {
        public bool IsOrdered { get; set; }
        public List<ListItemElement> Items { get; set; } = [];
    }

    public class ListItemElement : MarkdownElement
    {
        public required string Text { get; set; }
        public required int IndentationLevel { get; set; }
        public List<ListItemElement> Children { get; set; } = [];
        public List<MarkdownElement> Inlines { get; set; } = [];
    }

    public class QuoteElement : MarkdownElement
    {
        public required string Text { get; set; }
        public List<MarkdownElement> Inlines { get; set; } = [];
    }

    public class HorizontalRuleElement : MarkdownElement;

    public class TableElement : MarkdownElement
    {
        public List<string> Headers { get; set; } = [];
        public List<List<string>> Rows { get; set; } = [];
    }

    public class EmphasisElement : MarkdownElement
    {
        public required string Text { get; set; }
        public bool IsStrong { get; set; }
    }

    public class CodeInlineElement : MarkdownElement
    {
        public required string Code { get; set; }
    }

    public class TaskListElement : MarkdownElement
    {
        public List<TaskListItemElement> Items { get; set; } = [];
    }

    public class TaskListItemElement : MarkdownElement
    {
        public required string Text { get; set; }
        public bool IsChecked { get; set; }
        public int Level { get; set; }
        public List<TaskListItemElement> Children { get; set; } = [];
        public List<MarkdownElement> Inlines { get; set; } = [];
    }

    public class MathBlockElement : MarkdownElement
    {
        public required string Content { get; set; }
    }

    public class MathInlineElement : MarkdownElement
    {
        public required string Content { get; set; }
    }
}
