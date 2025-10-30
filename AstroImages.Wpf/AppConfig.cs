using System.Text.Json;
using System.IO;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Application configuration class that stores user preferences and settings.
    /// This class can be serialized to/from JSON to persist settings between application runs.
    /// Uses properties with automatic getters/setters for easy JSON serialization.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// List of custom keywords to extract from filenames (e.g., "RMS", "HFR", "Stars").
        /// Auto-property with default value of empty list.
        /// </summary>
        public List<string> CustomKeywords { get; set; } = new List<string>();
        
        /// <summary>
        /// List of FITS header keywords to display as columns (e.g., "OBJECT", "EXPTIME", "FILTER").
        /// These are metadata fields within FITS files.
        /// </summary>
        public List<string> FitsKeywords { get; set; } = new List<string>();
        
        /// <summary>
        /// Whether to show the file size column in the file list.
        /// Default is false to keep the interface cleaner.
        /// </summary>
        public bool ShowSizeColumn { get; set; } = false;
        
        // Window state properties - these remember the window position and size
        
        /// <summary>
        /// X position of the main window on screen. NaN means use default positioning.
        /// </summary>
        public double WindowLeft { get; set; } = double.NaN;
        
        /// <summary>
        /// Y position of the main window on screen. NaN means use default positioning.
        /// </summary>
        public double WindowTop { get; set; } = double.NaN;
        
        /// <summary>
        /// Width of the main window in pixels.
        /// </summary>
        public double WindowWidth { get; set; } = 1200;
        
        /// <summary>
        /// Height of the main window in pixels.
        /// </summary>
        public double WindowHeight { get; set; } = 720;
        
        /// <summary>
        /// Window state as integer: 0 = Normal, 1 = Minimized, 2 = Maximized.
        /// Using int instead of enum for easier JSON serialization.
        /// </summary>
        public int WindowState { get; set; } = 0;
        
        /// <summary>
        /// Last directory used in the "Move Files" dialog.
        /// Remembers where user last moved files to for convenience.
        /// </summary>
        public string? LastMoveDirectory { get; set; } = null;
        
        /// <summary>
        /// Last directory opened by the user.
        /// Used to default the folder dialog to the last used location.
        /// </summary>
        public string? LastOpenDirectory { get; set; } = null;
        
        /// <summary>
        /// Whether to show the splash screen on application startup.
        /// Default is true (show splash screen). Set to false to skip it.
        /// </summary>
        public bool ShowSplashScreen { get; set; } = true;

        /// <summary>
        /// Static field containing the full path where the configuration file is stored.
        /// Uses the user's AppData folder which is standard for application settings on Windows.
        /// Example path: C:\Users\[username]\AppData\Roaming\AstroImages\app-config.json
        /// </summary>
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AstroImages",
            "app-config.json");

        /// <summary>
        /// Static method to load configuration from file or create default configuration.
        /// Static means you can call it without creating an AppConfig instance first.
        /// This is the main way to get an AppConfig object at application startup.
        /// </summary>
        /// <returns>AppConfig object with loaded settings, or default settings if loading fails</returns>
        public static AppConfig Load()
        {
            try
            {
                // Check if the configuration file exists
                if (File.Exists(ConfigFilePath))
                {
                    // Read the entire file as a string
                    var json = File.ReadAllText(ConfigFilePath);
                    
                    // Deserialize JSON string back into AppConfig object
                    // JsonSerializer.Deserialize converts JSON text back to C# objects
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    
                    // Return the loaded config, or new default if deserialization returned null
                    return config ?? new AppConfig();
                }
            }
            catch (Exception)
            {
                // If anything goes wrong (file corruption, permissions, etc.),
                // don't crash the app - just use default settings
            }
            
            // File doesn't exist or loading failed - return default configuration
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception)
            {
                // If config saving fails, continue silently
            }
        }
    }

    public static class FilenameParser
    {
        public static Dictionary<string, string> ExtractKeywordValues(string filename, IEnumerable<string> keywords)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(filename))
                return result;

            // Remove file extension and split on underscores
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var tokens = nameWithoutExtension.Split('_');

            // Find each keyword and extract the following token as its value
            foreach (var keyword in keywords)
            {
                for (int i = 0; i < tokens.Length - 1; i++) // -1 because we need a following token
                {
                    if (tokens[i].Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        result[keyword] = tokens[i + 1];
                        break; // Take the first occurrence
                    }
                }
            }

            return result;
        }
    }
}