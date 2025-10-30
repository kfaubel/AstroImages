using System.Windows;

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
                
                // Create the main application window first
                // The window will apply saved position/size settings from config automatically
                var win = new MainWindow();
                
                // Set this as the application's main window - WPF uses this to know when to exit
                this.MainWindow = win;
                
                // Make the window visible to the user
                win.Show();

                // Load application configuration to check splash screen preference
                var config = AppConfig.Load();
                
                // Show splash screen as a modal dialog if enabled in configuration
                if (config.ShowSplashScreen)
                {
                    // Create the splash screen window
                    var splash = new SplashWindow();
                    
                    // Set the main window as the "owner" - this makes the splash appear on top
                    // and centers it relative to the main window
                    splash.Owner = win;
                    
                    // ShowDialog() makes it modal - user must interact with splash before continuing
                    // Returns true if user clicked OK, false if they cancelled or closed it
                    var result = splash.ShowDialog();
                    
                    // If user clicked OK and checked "Don't show again"
                    if (result == true && splash.DontShowAgain)
                    {
                        try
                        {
                            // Update configuration to disable splash screen for next time
                            config.ShowSplashScreen = false;
                            config.Save();
                        }
                        catch
                        {
                            // If we can't save config, just ignore it
                            // Don't crash the app over a preference setting
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If anything goes wrong during startup, show an error and exit gracefully
                // $"{ex}" is string interpolation - puts the exception details in the message
                System.Windows.MessageBox.Show($"Startup Exception: {ex}", "App Startup Error");
                Environment.Exit(1); // Exit with error code 1
            }
        }
    }
}