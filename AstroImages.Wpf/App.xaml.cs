using System.Windows;
using System.IO;
using AstroImages.Wpf.Services;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Main Application class - this is the entry point for the WPF application.
    /// Inherits from System.Windows.Application which provides the basic application framework.
    /// The "partial" keyword means this class is split across multiple files (App.xaml and App.xaml.cs).
    /// </summary>
    public partial class App : System.Windows.Application
    {

        /// <summary>
        /// Constructor - sets up global exception handling
        /// </summary>
        public App()
        {
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += (sender, e) =>
            {
                try
                {
                    System.Windows.MessageBox.Show(
                        $"Unhandled Exception:\n\nMessage: {e.Exception.Message}\n\nStack Trace: {e.Exception.StackTrace}",
                        "AstroImages - Unhandled Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { }
                e.Handled = true;
            };
        }

        /// <summary>
        /// OnStartup is called when the application starts up.
        /// This is where we initialize the main window and handle the splash screen.
        /// Override means we're replacing the base class implementation with our own.
        /// </summary>
        /// <param name="e">Startup arguments passed to the application</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Always call the base class method first - this initializes the WPF framework
                base.OnStartup(e);
                
                // Initialize the theme service before creating any windows
                ThemeService.Initialize();
                
                // Extract file paths from command-line arguments (if any)
                // When user opens files with our app from Explorer, paths are passed as arguments
                string[]? filePaths = e.Args.Length > 0 ? e.Args : null;
                
                // Create the main application window first
                // The window will apply saved position/size settings from config automatically
                // Pass file paths if provided so it can load only those files
                var win = new MainWindow(filePaths);
                
                // Set this as the application's main window - WPF uses this to know when to exit
                this.MainWindow = win;
                
                // Make the window visible to the user
                win.Show();

                // Load application configuration to check splash screen preference
                var config = AppConfig.Load();
                
                // Check for updates asynchronously (non-blocking)
                _ = CheckForUpdatesAsync(config, win);
            }
            catch (Exception ex)
            {
                // If anything goes wrong during startup, show an error and exit gracefully
                // $"{ex}" is string interpolation - puts the exception details in the message
                try
                {
                    System.Windows.MessageBox.Show(
                        $"Startup Exception:\n\nMessage: {ex.Message}\n\nStack Trace: {ex.StackTrace}\n\nInner Exception: {ex.InnerException?.Message ?? "None"}", 
                        "AstroImages - App Startup Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
                catch
                {
                    // If even MessageBox fails, write to console and try to create a log file
                    Console.WriteLine($"Fatal startup error: {ex}");
                    try
                    {
                        var logFile = Path.Combine(Path.GetTempPath(), "AstroImages_Error.log");
                        File.WriteAllText(logFile, $"AstroImages Startup Error: {DateTime.Now}\n\n{ex}");
                        Console.WriteLine($"Error details written to: {logFile}");
                    }
                    catch { }
                }
                Environment.Exit(1); // Exit with error code 1
            }
        }

        /// <summary>
        /// Asynchronously check for updates from GitHub releases
        /// </summary>
        private async Task CheckForUpdatesAsync(AppConfig config, Window mainWindow)
        {
            try
            {
                // Only check if enabled and not checked recently (within 24 hours)
                if (!config.CheckForUpdates) return;
                
                var now = DateTime.Now;
                if (config.LastUpdateCheck.HasValue && 
                    now - config.LastUpdateCheck.Value < TimeSpan.FromHours(24))
                {
                    return; // Already checked recently
                }

                // Create update service with repository information
                using var updateService = new UpdateService(config.UpdateRepoOwner, config.UpdateRepoName);
                
                // Check for updates (this is async and won't block startup)
                var updateInfo = await updateService.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                        // Update found - show dialog on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var updateDialog = new UpdateDialog(updateInfo, updateService);
                        updateDialog.Owner = mainWindow;
                        
                        var result = updateDialog.ShowDialog();
                        
                        // Update config based on user's choice
                        if (updateDialog.DisableUpdateChecks)
                        {
                            config.CheckForUpdates = false;
                        }
                        
                        // Record that we checked for updates
                        config.LastUpdateCheck = now;
                        config.Save();
                    });
                }
                else
                {
                    // No update available - just record the check time
                    config.LastUpdateCheck = now;
                    config.Save();
                }
            }
            catch (Exception ex)
            {
                // Don't let update check failures crash the app
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }
    }
}