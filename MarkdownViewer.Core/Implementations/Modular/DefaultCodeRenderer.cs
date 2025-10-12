using Avalonia.Controls;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Renderers;

namespace MarkdownViewer.Core.Implementations.Modular;

public class DefaultCodeRenderer : ICodeRenderer
{
    public Control? RenderCodeBlock(IModularMarkdownRenderer markdownRenderer, Control markdownControl, CodeBlockElement element)
    {
        var grid = new Grid();
        
        var border = new Border
        {
            Classes = { DefaultClasses.Markdown, DefaultClasses.Code, DefaultClasses.Block },
            Child = DefaultUtils.CreateTextBlock(element.Code, [DefaultClasses.Markdown, DefaultClasses.Code, DefaultClasses.Block])
        };
        
        grid.Children.Add(border);
        
        return grid;
    }

    public Control? RenderCodeInline(IModularMarkdownRenderer markdownRenderer, Control markdownControl, CodeInlineElement element)
    {
        return new Border
        {
            Classes = { DefaultClasses.Markdown, DefaultClasses.Code, DefaultClasses.Inline },
            Child = DefaultUtils.CreateTextBlock(element.Code, [DefaultClasses.Markdown, DefaultClasses.Code, DefaultClasses.Inline])
        };
    }
}