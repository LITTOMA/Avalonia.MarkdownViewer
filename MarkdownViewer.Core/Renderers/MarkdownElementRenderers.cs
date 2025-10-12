using Avalonia.Controls;
using MarkdownViewer.Core.Elements;
using TextElement = MarkdownViewer.Core.Elements.TextElement;

namespace MarkdownViewer.Core.Renderers;

public interface ITextRenderer
{
    Control? RenderText(IModularMarkdownRenderer markdownRenderer, Control markdownControl, TextElement element);
    Control? RenderHeading(IModularMarkdownRenderer markdownRenderer, Control markdownControl, HeadingElement element);
    Control? RenderEmphasis(IModularMarkdownRenderer markdownRenderer, Control markdownControl, EmphasisElement element);
}

public interface ICodeRenderer
{
    Control? RenderCodeBlock(IModularMarkdownRenderer markdownRenderer, Control markdownControl, CodeBlockElement element);
    Control? RenderCodeInline(IModularMarkdownRenderer markdownRenderer, Control markdownControl, CodeInlineElement element);
}

public interface IImageRenderer
{
    Control? RenderImage(IModularMarkdownRenderer markdownRenderer, Control markdownControl, ImageElement element);
}

public interface ILinkRenderer
{
    Control? RenderLink(IModularMarkdownRenderer markdownRenderer, Control markdownControl, LinkElement element);
}

public interface IListRenderer
{
    Control? RenderList(IModularMarkdownRenderer markdownRenderer, Control markdownControl, ListElement element);
}

public interface IQuoteRenderer
{
    Control? RenderQuote(IModularMarkdownRenderer markdownRenderer, Control markdownControl, QuoteElement element);
}

public interface IHorizontalRuleRenderer
{
    Control? RenderHorizontalRule(IModularMarkdownRenderer markdownRenderer, Control markdownControl, HorizontalRuleElement element);
}

public interface ITableRenderer
{
    Control? RenderTable(IModularMarkdownRenderer markdownRenderer, Control markdownControl, TableElement element);
}

public interface ITaskListRenderer
{
    Control? RenderTaskList(IModularMarkdownRenderer markdownRenderer, Control markdownControl, TaskListElement element);
}

public interface IMathRenderer
{
    Control? RenderMathBlock(IModularMarkdownRenderer markdownRenderer, Control markdownControl, MathBlockElement element);
    Control? RenderMathInline(IModularMarkdownRenderer markdownRenderer, Control markdownControl, MathInlineElement element);
}
