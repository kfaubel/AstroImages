using System;
using System.Windows;

namespace AstroImages.Wpf.Services
{
    public class ConfigurationService
    {
        public AppConfig LoadConfiguration()
        {
            return AppConfig.Load();
        }

        public void SaveConfiguration(AppConfig config)
        {
            config.Save();
        }

        public void SaveWindowState(Window window, AppConfig config)
        {
            // Only save position and size if the window is in Normal state
            if (window.WindowState == WindowState.Normal)
            {
                config.WindowLeft = window.Left;
                config.WindowTop = window.Top;
                config.WindowWidth = window.Width;
                config.WindowHeight = window.Height;
            }
            
            config.WindowState = (int)window.WindowState;
            config.Save();
        }

        public void RestoreWindowState(Window window, AppConfig config)
        {
            // Restore window size (with minimum constraints)
            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                window.Width = Math.Max(config.WindowWidth, window.MinWidth);
                window.Height = Math.Max(config.WindowHeight, window.MinHeight);
            }

            // Restore window position if valid
            if (!double.IsNaN(config.WindowLeft) && !double.IsNaN(config.WindowTop))
            {
                // Check if the position is within reasonable screen bounds
                var left = config.WindowLeft;
                var top = config.WindowTop;
                
                // Ensure the window is at least partially visible on screen
                if (left < SystemParameters.VirtualScreenWidth - 100 && 
                    top < SystemParameters.VirtualScreenHeight - 100 &&
                    left > -window.Width + 100 && 
                    top > -50) // Allow negative values for multi-monitor setups
                {
                    window.Left = left;
                    window.Top = top;
                }
            }

            // Restore window state (but avoid restoring Minimized state)
            if (config.WindowState >= 0 && config.WindowState <= 2)
            {
                var state = (WindowState)config.WindowState;
                if (state != WindowState.Minimized) // Don't restore minimized state
                {
                    window.WindowState = state;
                }
            }
        }
    }
}