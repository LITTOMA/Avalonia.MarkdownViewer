using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Media;
using System;
using Avalonia.Data;

namespace MarkdownViewer.Core
{
    /// <summary>
    /// Markdown theme resource manager
    /// </summary>
    public static class MarkdownTheme
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new();
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
                    if (Application.Current?.Styles != null)
                    {
                        var themeUri = new Uri("avares://MarkdownViewer.Core/Themes/MarkdownTheme.axaml");
                        if (AvaloniaXamlLoader.Load(themeUri) is IStyle theme)
                        {
                            bool themeExists = Application.Current.Styles
                                .Any(style => style.GetType().Name.Contains("MarkdownTheme"));
                            
                            if (!themeExists)
                            {
                                Application.Current.Styles.Add(theme);
                            }
                        }
                        
                        var modularThemeUri = new Uri("avares://MarkdownViewer.Core/Themes/ModularMarkdownTheme.axaml");
                        if (AvaloniaXamlLoader.Load(modularThemeUri) is IStyle modularTheme)
                        {
                            bool themeExists = Application.Current.Styles
                                .Any(style => style.GetType().Name.Contains("ModularMarkdownTheme"));
                            
                            if (!themeExists)
                            {
                                Application.Current.Styles.Add(modularTheme);
                            }
                        }
                        
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
            if (e.Property != Application.ActualThemeVariantProperty) 
                return;
            
            var newTheme = e.NewValue as ThemeVariant;
            if (newTheme != _lastThemeVariant)
            {
                _lastThemeVariant = newTheme;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
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
            return GetResource(resourceKey, () => new SolidColorBrush(fallbackColor));
        }

        public static T GetResource<T>(string resourceKey, Func<T> defaultValue)
        {
            // Ensure theme is initialized
            Initialize();

            // Try to get from application resources
            if (Application.Current?.TryGetResource(
                    resourceKey,
                    Application.Current.ActualThemeVariant,
                    out object? resource
                ) == true
            )
            {
                if (resource is T value)
                    return value;
                
            }

            return defaultValue();
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
