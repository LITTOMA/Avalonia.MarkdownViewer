using Avalonia.Controls;
using Avalonia.Media;

namespace MarkdownViewer.Core.Implementations.Modular;

public class DefaultUtils
{
    public static bool UseSelectableText { get; set; } = true;
    
    public static TextBlock CreateTextBlock(string? text, string[] classes)
    {
        TextBlock textBlock = UseSelectableText
            ? new SelectableTextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            }
            : new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
        
        textBlock.Classes.AddRange(classes);
        return textBlock;
    }
}