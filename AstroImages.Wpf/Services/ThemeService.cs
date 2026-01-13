using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AstroImages.Wpf.Services
{
    /// <summary>
    /// Service responsible for managing application themes (light/dark mode).
    /// Provides functionality to detect OS theme preference and apply appropriate themes.
    /// </summary>
    public static class ThemeService
    {
        /// <summary>
        /// Event raised when the theme changes, allowing UI components to respond.
        /// </summary>
        public static event EventHandler? ThemeChanged;

        /// <summary>
        /// Gets the current theme mode setting.
        /// </summary>
        public static ThemeMode CurrentThemeMode { get; private set; } = ThemeMode.Auto;

        /// <summary>
        /// Gets whether the current effective theme is dark (true) or light (false).
        /// </summary>
        public static bool IsDarkTheme { get; private set; } = false;

        /// <summary>
        /// Initializes the theme service and applies the initial theme from AppConfig.
        /// </summary>
        public static void Initialize()
        {
            var appConfig = AppConfig.Load();
            SetThemeMode(appConfig.Theme);
        }

        /// <summary>
        /// Changes the theme mode and applies the new theme.
        /// </summary>
        /// <param name="themeMode">The new theme mode to apply</param>
        public static void SetThemeMode(ThemeMode themeMode)
        {
            // Always apply theme on initialization or when theme changes
            bool needsInitialization = !System.Windows.Application.Current.Resources.Contains("ThemeWindowBackground");
            
            if (CurrentThemeMode != themeMode || needsInitialization)
            {
                CurrentThemeMode = themeMode;
                ApplyTheme();
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Detects whether the OS is using dark theme.
        /// </summary>
        /// <returns>True if OS is using dark theme, false otherwise</returns>
        public static bool IsOSDarkTheme()
        {
            try
            {
                // Check Windows registry for apps theme setting
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
                {
                    return appsUseLightTheme == 0; // 0 = dark theme, 1 = light theme
                }
            }
            catch
            {
                // If we can't read registry, default to light theme
            }

            return false; // Default to light theme if detection fails
        }

        /// <summary>
        /// Applies the current theme to the application.
        /// </summary>
        private static void ApplyTheme()
        {
            // Determine if we should use dark theme
            bool useDarkTheme = CurrentThemeMode switch
            {
                ThemeMode.Auto => IsOSDarkTheme(),
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                _ => false
            };

            IsDarkTheme = useDarkTheme;

            // Get the application's resource dictionary
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // Clear existing theme resources
            var resourcesToRemove = app.Resources.Keys.Cast<object>()
                .Where(key => key.ToString()?.StartsWith("Theme") == true)
                .ToList();

            foreach (var key in resourcesToRemove)
            {
                app.Resources.Remove(key);
            }

            // Apply theme-specific resources
            if (useDarkTheme)
            {
                ApplyDarkTheme(app);
            }
            else
            {
                ApplyLightTheme(app);
            }
        }

        /// <summary>
        /// Applies dark theme resources to the application.
        /// </summary>
        /// <param name="app">The application instance</param>
        private static void ApplyDarkTheme(System.Windows.Application app)
        {
            // Window and control backgrounds
            app.Resources["ThemeWindowBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 32, 32));           // #202020
            app.Resources["ThemeControlBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));          // #2D2D2D
            app.Resources["ThemeListViewBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40));         // #282828
            app.Resources["ThemeSidebarBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 35, 35));          // #232323
            app.Resources["ThemeHeaderBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));           // #404040

            // Text colors
            app.Resources["ThemeForeground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));              // #F0F0F0
            app.Resources["ThemeSecondaryText"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));           // #C8C8C8
            app.Resources["ThemeForegroundDisabled"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));      // #808080

            // Border colors
            app.Resources["ThemeBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 70, 70));                // #464646
            app.Resources["ThemeMenuBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));            // #3C3C3C

            // Selection colors
            app.Resources["ThemeSelectionBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));       // #0078D7 (Windows accent)
            app.Resources["ThemeSelectionForeground"] = new SolidColorBrush(System.Windows.Media.Colors.White);

            // TextBox colors
            app.Resources["ThemeTextBoxBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));          // #323232

            // Button colors
            app.Resources["ThemeButtonBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));           // #3C3C3C
            app.Resources["ThemeButtonForeground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));        // #F0F0F0
            app.Resources["ThemeButtonBackgroundHover"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));      // #505050
            app.Resources["ThemeButtonBackgroundPressed"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40));    // #282828
            app.Resources["ThemeButtonBackgroundDisabled"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));   // #2D2D2D

            // Code block colors (for documentation)
            app.Resources["ThemeCodeBlockBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));        // #3C3C3C

            // Accent colors for file list - dark mode (bright and vibrant)
            app.Resources["ThemeAccentBlue"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(135, 206, 255));               // #87CEFF (brighter blue for dark mode)
            app.Resources["ThemeAccentGreen"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 238, 144));              // #90EE90 (medium light green for dark mode)
            app.Resources["ThemeAccent"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));                      // #0078D7 (Windows accent blue for headers)

            // ListView item selection and hover colors
            app.Resources["ThemeListViewHoverBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 55, 55));     // #373737 (darker hover)
            app.Resources["ThemeListViewSelectionBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50)); // #323232 (darker selection)
            app.Resources["ThemeListViewSelectionForeground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)); // #F0F0F0

            // Image viewer area background
            app.Resources["ThemeImageAreaBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));          // #323232 (gray for dark mode)

            // ScrollBar colors
            app.Resources["ThemeScrollBarBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));          // #2D2D2D
            app.Resources["ThemeScrollBarThumb"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));            // #646464
        }

        /// <summary>
        /// Applies light theme resources to the application.
        /// </summary>
        /// <param name="app">The application instance</param>
        private static void ApplyLightTheme(System.Windows.Application app)
        {
            // Window and control backgrounds
            app.Resources["ThemeWindowBackground"] = new SolidColorBrush(System.Windows.Media.Colors.White);                       // #FFFFFF
            app.Resources["ThemeControlBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));      // #FAFAFA
            app.Resources["ThemeListViewBackground"] = new SolidColorBrush(System.Windows.Media.Colors.White);                     // #FFFFFF
            app.Resources["ThemeSidebarBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248));      // #F8F8F8
            app.Resources["ThemeHeaderBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248));       // #F8F8F8

            // Text colors
            app.Resources["ThemeForeground"] = new SolidColorBrush(System.Windows.Media.Colors.Black);                             // #000000
            app.Resources["ThemeSecondaryText"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));          // #646464
            app.Resources["ThemeForegroundDisabled"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160));     // #A0A0A0

            // Border colors
            app.Resources["ThemeBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));            // #C8C8C8
            app.Resources["ThemeMenuBorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));        // #DCDCDC

            // Selection colors
            app.Resources["ThemeSelectionBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));       // #0078D7 (Windows accent)
            app.Resources["ThemeSelectionForeground"] = new SolidColorBrush(System.Windows.Media.Colors.White);

            // TextBox colors
            app.Resources["ThemeTextBoxBackground"] = new SolidColorBrush(System.Windows.Media.Colors.White);                       // #FFFFFF

            // Button colors
            app.Resources["ThemeButtonBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));        // #F0F0F0
            app.Resources["ThemeButtonForeground"] = new SolidColorBrush(System.Windows.Media.Colors.Black);                        // #000000
            app.Resources["ThemeButtonBackgroundHover"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));   // #E6E6E6
            app.Resources["ThemeButtonBackgroundPressed"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)); // #D2D2D2
            app.Resources["ThemeButtonBackgroundDisabled"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248)); // #F8F8F8

            // Code block colors (for documentation)
            app.Resources["ThemeCodeBlockBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 248, 248));      // #F8F8F8

            // Accent colors for file list - light mode (distinct and readable with more color saturation)
            app.Resources["ThemeAccentBlue"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 120, 180));                   // #3278B4 (brighter blue for light mode)
            app.Resources["ThemeAccentGreen"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 0));                    // #009600 (brighter green for light mode)
            app.Resources["ThemeAccent"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 115, 207));                      // #0073CF (medium blue for light mode - good contrast for headers)

            // ListView item selection and hover colors
            app.Resources["ThemeListViewHoverBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));  // #F0F0F0 (light hover)
            app.Resources["ThemeListViewSelectionBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)); // #0078D7 (Windows accent)
            app.Resources["ThemeListViewSelectionForeground"] = new SolidColorBrush(System.Windows.Media.Colors.White);              // White text on selection

            // Image viewer area background
            app.Resources["ThemeImageAreaBackground"] = new SolidColorBrush(System.Windows.Media.Colors.White);                      // White for light mode

            // ScrollBar colors
            app.Resources["ThemeScrollBarBackground"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));      // #F0F0F0
            app.Resources["ThemeScrollBarThumb"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160));           // #A0A0A0
        }
    }
}