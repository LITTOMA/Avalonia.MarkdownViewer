using Avalonia.Controls;
using AvaloniaMath.Controls;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Renderers;

namespace MarkdownViewer.Core.Implementations.Modular;

public class DefaultMathRenderer : IMathRenderer
{
    public Control? RenderMathBlock(IModularMarkdownRenderer markdownRenderer, Control markdownControl, MathBlockElement element)
    {
        return new FormulaBlock
        {
            Formula = element.Content,
            Classes = { DefaultClasses.Markdown, DefaultClasses.Math, DefaultClasses.Block }
        };
    }

    public Control? RenderMathInline(IModularMarkdownRenderer markdownRenderer, Control markdownControl, MathInlineElement element)
    {
        return new FormulaBlock
        {
            Formula = element.Content,
            Classes = { DefaultClasses.Markdown, DefaultClasses.Math, DefaultClasses.Inline }
        };
    }
}