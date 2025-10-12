using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using MarkdownViewer.Core.Elements;
using TextElement = MarkdownViewer.Core.Elements.TextElement;

namespace MarkdownViewer.Core.Renderers;

public interface IModularMarkdownRenderer
{
    ITextRenderer TextRenderer { get; }
    ICodeRenderer CodeRenderer { get; }
    ILinkRenderer LinkRenderer { get; }
    IQuoteRenderer QuoteRenderer { get; }
    IImageRenderer ImageRenderer { get; }
    IHorizontalRuleRenderer HorizontalRuleRenderer { get; }
    IListRenderer ListRenderer { get; }
    ITableRenderer TableRenderer { get; }
    ITaskListRenderer TaskListRenderer { get; }
    IMathRenderer MathRenderer { get; }
}

public static class ModularMarkdownRendererExtensions
{
    public static Control? RenderElement(
        this IModularMarkdownRenderer markdownRenderer, 
        Control markdownControl, 
        MarkdownElement element)
    {
        try
        {
            return element switch
            {
                ParagraphElement paragraphElement =>
                    RenderParagraph(markdownRenderer, markdownControl, paragraphElement),
                TextElement textElement =>
                    markdownRenderer.TextRenderer.RenderText(markdownRenderer, markdownControl, textElement),
                HeadingElement headingElement =>
                    markdownRenderer.TextRenderer.RenderHeading(markdownRenderer, markdownControl, headingElement),
                EmphasisElement emphasisElement =>
                    markdownRenderer.TextRenderer.RenderEmphasis(markdownRenderer, markdownControl, emphasisElement),
                CodeBlockElement codeBlockElement =>
                    markdownRenderer.CodeRenderer.RenderCodeBlock(markdownRenderer, markdownControl, codeBlockElement),
                CodeInlineElement codeInlineElement =>
                    markdownRenderer.CodeRenderer.RenderCodeInline(markdownRenderer, markdownControl, codeInlineElement),
                LinkElement linkElement =>
                    markdownRenderer.LinkRenderer.RenderLink(markdownRenderer, markdownControl, linkElement),
                QuoteElement quoteElement =>
                    markdownRenderer.QuoteRenderer.RenderQuote(markdownRenderer, markdownControl, quoteElement),
                ImageElement imageElement =>
                    markdownRenderer.ImageRenderer.RenderImage(markdownRenderer, markdownControl, imageElement),
                HorizontalRuleElement horizontalRuleElement =>
                    markdownRenderer.HorizontalRuleRenderer.RenderHorizontalRule(markdownRenderer, markdownControl, horizontalRuleElement),
                ListElement listElement =>
                    markdownRenderer.ListRenderer.RenderList(markdownRenderer, markdownControl, listElement),
                TableElement tableElement =>
                    markdownRenderer.TableRenderer.RenderTable(markdownRenderer, markdownControl, tableElement),
                TaskListElement taskListElement =>
                    markdownRenderer.TaskListRenderer.RenderTaskList(markdownRenderer, markdownControl, taskListElement),
                MathBlockElement mathBlockElement =>
                    markdownRenderer.MathRenderer.RenderMathBlock(markdownRenderer, markdownControl, mathBlockElement),
                MathInlineElement mathInlineElement =>
                    markdownRenderer.MathRenderer.RenderMathInline(markdownRenderer, markdownControl, mathInlineElement),
                _ => null
            };
        }
        catch (Exception e)
        {
            return new TextBlock
            {
                Text = e.Message,
                Foreground = Brushes.Red
            };
        }
    }

    public static Control? RenderInlineElement(
        this IModularMarkdownRenderer markdownRenderer,
        Control markdownControl,
        TextBlock textBlock,
        MarkdownElement element)
    {
        if (textBlock.Inlines == null)
            return null;
        
        Control? inlineControl = null;
        try
        {
            switch (element)
            {
                case TextElement textElement:
                    inlineControl = markdownRenderer.TextRenderer.RenderText(markdownRenderer, markdownControl, textElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
                case EmphasisElement emphasisElement:
                    inlineControl = markdownRenderer.TextRenderer.RenderEmphasis(markdownRenderer, markdownControl, emphasisElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
                case CodeInlineElement codeInlineElement:
                    inlineControl = markdownRenderer.CodeRenderer.RenderCodeInline(markdownRenderer, markdownControl, codeInlineElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
                case LinkElement linkElement:
                    inlineControl = markdownRenderer.LinkRenderer.RenderLink(markdownRenderer, markdownControl, linkElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
                case ImageElement imageElement:
                    inlineControl = markdownRenderer.ImageRenderer.RenderImage(markdownRenderer, markdownControl, imageElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
                case MathInlineElement mathInlineElement:
                    inlineControl = markdownRenderer.MathRenderer.RenderMathInline(markdownRenderer, markdownControl, mathInlineElement);
                    TryAddInline(textBlock, inlineControl);
                    break;
            }
        }
        catch (Exception e)
        {
            inlineControl = new TextBlock
            {
                Text = e.Message,
                Foreground = Brushes.Red
            };
            TryAddInline(textBlock, inlineControl);
        }
        
        return inlineControl;
    }

    static Control? RenderParagraph(
        IModularMarkdownRenderer markdownRenderer,
        Control markdownControl,
        ParagraphElement paragraphElement)
    {
        if (paragraphElement.Inlines is [ImageElement imageElement])
            return markdownRenderer.ImageRenderer.RenderImage(markdownRenderer, markdownControl, imageElement);
        
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        foreach (var inlineElement in paragraphElement.Inlines)
            markdownRenderer.RenderInlineElement(markdownControl, textBlock, inlineElement);

        return textBlock;
    }

    static void TryAddInline(TextBlock textBlock, Control? inlineControl)
    {
        if (inlineControl != null)
            textBlock.Inlines!.Add(new InlineUIContainer(inlineControl));
    }
}