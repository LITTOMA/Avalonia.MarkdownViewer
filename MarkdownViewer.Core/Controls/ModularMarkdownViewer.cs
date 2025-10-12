using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using MarkdownViewer.Core.Implementations;
using MarkdownViewer.Core.Implementations.Modular;
using MarkdownViewer.Core.Renderers;

namespace MarkdownViewer.Core.Controls;

public enum ModularMarkdownViewerRenderType
{
    Blocking,
    AsyncLoad,
    AsyncRender
}

public class ModularMarkdownViewer : ContentControl
{
    public static IModularMarkdownRenderer FallbackRenderer { get; set; } = new DefaultModularMarkdownRenderer();
    
    public IModularMarkdownRenderer Renderer { get; init; } = FallbackRenderer;
    public ModularMarkdownViewerRenderType RenderType { get; init; } = ModularMarkdownViewerRenderType.Blocking;
    
    private string _markdownText = string.Empty;
    
    /// <summary>
    /// Gets or sets the Markdown text to be displayed.
    /// </summary>
    public static readonly DirectProperty<ModularMarkdownViewer, string> MarkdownTextProperty =
        AvaloniaProperty.RegisterDirect<ModularMarkdownViewer, string>(
            nameof(MarkdownText),
            getter: obj => obj.MarkdownText,
            setter: (obj, value) => obj.MarkdownText = value,
            defaultBindingMode: BindingMode.TwoWay
        );

    /// <summary>
    /// Gets or sets the Markdown text to be displayed.
    /// </summary>
    public string MarkdownText
    {
        get => _markdownText;
        set => SetAndRaise(MarkdownTextProperty, ref _markdownText, value);
    }

    public ModularMarkdownViewer()
    {
        Classes.Add(DefaultClasses.Markdown);
    }
    
    /// <summary>
    /// Called when a property value is changed.
    /// </summary>
    /// <param name="change">A value containing event data.</param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownTextProperty)
            UpdateContent();
    }

    void UpdateContent()
    {
        switch (RenderType)
        {
            case ModularMarkdownViewerRenderType.Blocking:
                RenderContent();
                break;
            case ModularMarkdownViewerRenderType.AsyncLoad:
                _ = RenderContentAsync();
                break;
            case ModularMarkdownViewerRenderType.AsyncRender:
                _ = RenderContentRealtimeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void RenderContent()
    {
        var parser = new MarkdigParser();
        var elements = parser.ParseTextAsync(_markdownText).ToBlockingEnumerable();
        var panel = new StackPanel { Orientation = Orientation.Vertical };

        foreach (var element in elements)
        {
            Control? control = Renderer.RenderElement(this, element);
            if (control != null)
                panel.Children.Add(control);
        }

        Content = panel;
    }

    // Async loading but waits until fully complete before rendering.
    private async Task RenderContentAsync(CancellationToken cancellationToken = default)
    {
        var parser = new MarkdigParser();
        var panel = new StackPanel { Orientation = Orientation.Vertical };

        await foreach (var element in parser.ParseTextAsync(_markdownText, cancellationToken))
        {
            Control? control = Renderer.RenderElement(this, element);
            if (control != null)
                panel.Children.Add(control);
        }

        Content = panel;
    }
    
    // Async loading and refreshes UI in chunks.
    private async Task RenderContentRealtimeAsync(CancellationToken cancellationToken = default)
    {
        var parser = new MarkdigParser();
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        Content = panel;

        await foreach (var element in parser.ParseTextAsync(_markdownText, cancellationToken))
        {
            Control? control = Renderer.RenderElement(this, element);
            if (control != null) // does this dirty it automatically? hopefully it does.
                panel.Children.Add(control);
        }
    }
}