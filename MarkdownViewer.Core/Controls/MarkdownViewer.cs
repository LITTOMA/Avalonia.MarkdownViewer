using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using MarkdownViewer.Core.Implementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MarkdownViewer.Core.Services;

namespace MarkdownViewer.Core.Controls
{
    /// <summary>
    /// A control for displaying and rendering Markdown content.
    /// </summary>
    public class MarkdownViewer : ContentControl
    {
        private static readonly IMarkdownRenderer DefaultRenderer;
        private IMarkdownRenderer? _renderer;
        private string markdownText = string.Empty;

        static MarkdownViewer()
        {
            // Initialize theme resources
            MarkdownTheme.Initialize();

            var httpClient = new HttpClient();
            var imageCacheLogger = NullLogger<MemoryImageCache>.Instance;
            var imageCache = new MemoryImageCache(httpClient, imageCacheLogger);
            DefaultRenderer = new AvaloniaMarkdownRenderer(
                imageCache,
                NullLogger<AvaloniaMarkdownRenderer>.Instance
            );
        }

        /// <summary>
        /// Gets or sets the Markdown text to be displayed.
        /// </summary>
        public static readonly DirectProperty<MarkdownViewer, string> MarkdownTextProperty =
            AvaloniaProperty.RegisterDirect<MarkdownViewer, string>(
                nameof(MarkdownText),
                getter: obj => obj.MarkdownText,
                setter: (obj, value) => obj.MarkdownText = value,
                defaultBindingMode: BindingMode.TwoWay
            );

        /// <summary>
        /// Gets or sets the Markdown renderer property.
        /// </summary>
        public static readonly StyledProperty<IMarkdownRenderer> RendererProperty =
            AvaloniaProperty.Register<MarkdownViewer, IMarkdownRenderer>(
                nameof(Renderer),
                coerce: (obj, value) =>
                {
                    if (obj is MarkdownViewer viewer)
                    {
                        viewer._renderer = value;
                        viewer.RenderContent();
                    }
                    return value;
                }
            );

        /// <summary>
        /// Gets or sets the Markdown text to be displayed.
        /// </summary>
        public string MarkdownText
        {
            get => markdownText;
            set => SetAndRaise(MarkdownTextProperty, ref markdownText, value);
        }

        /// <summary>
        /// Gets or sets the markdown renderer used to convert markdown text to visual elements.
        /// </summary>
        public IMarkdownRenderer Renderer
        {
            get => GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        /// <summary>
        /// Called when a property value is changed.
        /// </summary>
        /// <param name="change">A value containing event data.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == MarkdownTextProperty)
            {
                RenderContent();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownViewer"/> class.
        /// </summary>
        public MarkdownViewer()
        {
            Renderer = DefaultRenderer;
        }

        private void RenderContent()
        {
            if (_renderer != null && !string.IsNullOrEmpty(MarkdownText))
            {
                Content = _renderer.RenderDocument(MarkdownText);
            }
        }
    }
}
