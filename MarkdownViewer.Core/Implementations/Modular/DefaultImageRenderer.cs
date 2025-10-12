using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Renderers;
using MarkdownViewer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarkdownViewer.Core.Implementations.Modular;

public class DefaultImageRenderer : IImageRenderer
{
    readonly ILogger _logger;
    readonly MemoryImageCache _imageCache;
    
    public DefaultImageRenderer()
    {
        var httpClient = new HttpClient();
        var imageCacheLogger = NullLogger<MemoryImageCache>.Instance;
        _logger = imageCacheLogger;
        _imageCache = new MemoryImageCache(httpClient, imageCacheLogger);
    }
    
    public Control? RenderImage(IModularMarkdownRenderer markdownRenderer, Control markdownControl, ImageElement element)
    {
        var img = new Image
        {
            Classes = { DefaultClasses.Markdown, DefaultClasses.Image },
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxHeight = 400
        };

        LoadImageAsync(img, element.Source);
        return img;
    }
    
    private async void LoadImageAsync(Image img, string source)
    {
        if (string.IsNullOrEmpty(source))
            return;

        try
        {
            var imageData = await _imageCache.GetImageAsync(source);
            using var stream = new MemoryStream(imageData);
            var bitmap = new Bitmap(stream);

            // Set image source on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                img.Source = bitmap;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading image from {Source}", source);
            img.Source = CreateErrorPlaceholder($"Error: {ex.Message}");
        }
    }
    
    private IImage CreateErrorPlaceholder(string message)
    {
        // Create a simple error placeholder
        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            context.DrawRectangle(
                Brushes.LightGray,
                new Pen(Brushes.Gray, 1),
                new Rect(0, 0, 100, 100)
            );

            var text = new FormattedText(
                message,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default),
                12,
                Brushes.Gray
            );

            context.DrawText(text, new Point(5, 40));
        }

        return new DrawingImage(drawingGroup);
    }
}