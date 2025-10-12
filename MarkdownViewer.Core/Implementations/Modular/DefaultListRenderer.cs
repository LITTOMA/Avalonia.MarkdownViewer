using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Renderers;

namespace MarkdownViewer.Core.Implementations.Modular;

public class DefaultListRenderer : IListRenderer, ITaskListRenderer
{
    public Control? RenderList(IModularMarkdownRenderer markdownRenderer, Control markdownControl, ListElement element)
    {
        var panel = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 10)
        };

        foreach (var item in element.Items)
        {
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(item.IndentationLevel * 20, 0, 0, 0),
                Spacing = 5
            };
            
            // Select different symbols based on level and list type
            string bulletText = element.IsOrdered
                ? $"{element.Items.IndexOf(item) + 1}."
                : (item.IndentationLevel == 0 ? "•" : "◦");
            
            var bullet = new TextBlock
            {
                Classes = { DefaultClasses.Markdown, DefaultClasses.List, DefaultClasses.ListBullet },
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
            
            var textBlock = DefaultUtils.CreateTextBlock(null, [DefaultClasses.Markdown, DefaultClasses.List, DefaultClasses.Text]);
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            
            foreach (MarkdownElement inline in item.Inlines)
                markdownRenderer.RenderInlineElement(markdownControl, textBlock, inline);
            
            contentPanel.Children.Add(textBlock);

            if (item.Children.Count > 0)
            {
                var subList = new ListElement
                {
                    RawText = item.RawText,
                    Items = item.Children,
                    IsOrdered = element.IsOrdered
                };
                
                Control? subListControl = RenderList(markdownRenderer, markdownControl, subList);
                if (subListControl != null)
                    contentPanel.Children.Add(subListControl);
            }
            
            itemPanel.Children.Add(bullet);
            itemPanel.Children.Add(contentPanel);
            panel.Children.Add(itemPanel);
        }

        return panel;
    }

    public Control? RenderTaskList(IModularMarkdownRenderer markdownRenderer, Control markdownControl, TaskListElement element)
    {
        throw new NotImplementedException();
    }
}