using System;
using System.Windows;
using AstroImages.Wpf.ViewModels;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Floating window for displaying histogram separate from the main window.
    /// Allows histogram viewing on a second monitor while keeping the file list on the primary monitor.
    /// </summary>
    public partial class FloatingHistogramWindow : Window
    {
        // Callback to notify main window when dock back is requested
        private readonly Action? _onDockBackRequested;

        /// <summary>
        /// Constructor for the floating histogram window.
        /// </summary>
        /// <param name="histogramViewModel">The histogram view model for data binding</param>
        /// <param name="onDockBackRequested">Callback to invoke when user wants to dock back</param>
        public FloatingHistogramWindow(HistogramViewModel histogramViewModel, Action onDockBackRequested)
        {
            InitializeComponent();
            
            _onDockBackRequested = onDockBackRequested;
            
            // Set the DataContext for the histogram control
            HistogramControl.DataContext = histogramViewModel;
            
            // Subscribe to closing event to dock back when window is closed
            Closing += FloatingHistogramWindow_Closing;
        }

        /// <summary>
        /// Handle window closing - just allow the window to close without docking back
        /// </summary>
        private void FloatingHistogramWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Just allow the window to close
            // The user can use the re-dock button in the main window to restore the histogram
        }
        
        /// <summary>
        /// Programmatically close this window (called from DockBack)
        /// </summary>
        public void CloseWindow()
        {
            Close();
        }
    }
}
