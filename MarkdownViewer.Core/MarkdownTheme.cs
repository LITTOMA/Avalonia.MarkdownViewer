using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Media;
using System;

namespace MarkdownViewer.Core
{
    /// <summary>
    /// Markdown theme resource manager
    /// </summary>
    public static class MarkdownTheme
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();
        private static ThemeVariant? _lastThemeVariant;

        /// <summary>
        /// Theme change event
        /// </summary>
        public static event EventHandler? ThemeChanged;

        /// <summary>
        /// Initialize Markdown theme resources
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    // Load theme resources
                    var themeUri = new Uri(
                        "avares://MarkdownViewer.Core/Themes/MarkdownTheme.axaml"
                    );
                    var theme = AvaloniaXamlLoader.Load(themeUri) as IStyle;

                    if (theme != null && Application.Current?.Styles != null)
                    {
                        // Check if theme has already been added
                        bool themeExists = false;
                        foreach (var style in Application.Current.Styles)
                        {
                            if (style.GetType().Name.Contains("MarkdownTheme"))
                            {
                                themeExists = true;
                                break;
                            }
                        }

                        if (!themeExists)
                        {
                            Application.Current.Styles.Add(theme);
                        }
                    }

                    // Listen for theme changes
                    if (Application.Current != null)
                    {
                        _lastThemeVariant = Application.Current.ActualThemeVariant;
                        Application.Current.PropertyChanged += OnApplicationPropertyChanged;
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    // If theme loading fails, log error but don't throw exception
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to load Markdown theme: {ex.Message}"
                    );
                }
            }
        }

        private static void OnApplicationPropertyChanged(
            object? sender,
            AvaloniaPropertyChangedEventArgs e
        )
        {
            if (e.Property == Application.ActualThemeVariantProperty)
            {
                var newTheme = e.NewValue as ThemeVariant;
                if (newTheme != _lastThemeVariant)
                {
                    _lastThemeVariant = newTheme;
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Get theme brush resource
        /// </summary>
        /// <param name="resourceKey">Resource key</param>
        /// <param name="fallbackColor">Fallback color</param>
        /// <returns>Brush resource</returns>
        public static IBrush GetThemeBrush(string resourceKey, Color fallbackColor)
        {
            // Ensure theme is initialized
            Initialize();

            // Try to get from application resources
            if (
                Application.Current?.TryGetResource(
                    resourceKey,
                    Application.Current.ActualThemeVariant,
                    out var resource
                ) == true
                && resource is IBrush brush
            )
            {
                return brush;
            }

            // Fallback to default color
            return new SolidColorBrush(fallbackColor);
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public static void Cleanup()
        {
            if (Application.Current != null)
            {
                Application.Current.PropertyChanged -= OnApplicationPropertyChanged;
            }
        }
    }
}
