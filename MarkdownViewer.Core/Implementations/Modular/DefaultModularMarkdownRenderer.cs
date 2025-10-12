using Avalonia.Controls;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Implementations.Modular;
using MarkdownViewer.Core.Renderers;

namespace MarkdownViewer.Core.Implementations;

public class DefaultModularMarkdownRenderer : 
    IModularMarkdownRenderer,
    IHorizontalRuleRenderer
{
    public ITextRenderer TextRenderer { get; } = new DefaultTextRenderer();
    public ICodeRenderer CodeRenderer { get; } = new DefaultCodeRenderer();
    public IImageRenderer ImageRenderer { get; } = new DefaultImageRenderer();
    public ILinkRenderer LinkRenderer { get; } = new DefaultLinkRenderer();
    public IQuoteRenderer QuoteRenderer { get; } = new DefaultTextRenderer();
    public IListRenderer ListRenderer { get; } = new DefaultListRenderer();
    public ITableRenderer TableRenderer { get; } = new DefaultTableRenderer();
    public ITaskListRenderer TaskListRenderer { get; } = new DefaultListRenderer();
    public IMathRenderer MathRenderer { get; } = new DefaultMathRenderer();
    public IHorizontalRuleRenderer HorizontalRuleRenderer => this;
    
    public Control? RenderHorizontalRule(IModularMarkdownRenderer markdownRenderer, Control markdownControl, HorizontalRuleElement element)
    {
        return new Border
        {
            Classes = { DefaultClasses.Markdown, DefaultClasses.HorizontalRule }
        };
    }
}