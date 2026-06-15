using System;
using System.Windows;
using System.Windows.Input;
using AstroImages.Wpf.ViewModels;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Floating window for displaying images separate from the main window.
    /// Allows image viewing on a second monitor while keeping the file list on the primary monitor.
    /// </summary>
    public partial class FloatingImageWindow : Window
    {
        private MainWindowViewModel? _viewModel;
        private System.Windows.Point? _lastMousePos = null;
        
        // Callback to notify main window when dock back is requested
        private readonly Action? _onDockBackRequested;
        
        // Track full screen state
        private bool _isFullScreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private double _previousLeft;
        private double _previousTop;
        private double _previousWidth;
        private double _previousHeight;

        /// <summary>
        /// Constructor for the floating image window.
        /// </summary>
        /// <param name="viewModel">The main window's view model for data binding</param>
        /// <param name="onDockBackRequested">Callback to invoke when user wants to dock back</param>
        /// <param name="initialWidth">The initial width for the window (typically from the main window's image area)</param>
        public FloatingImageWindow(MainWindowViewModel viewModel, Action onDockBackRequested, double initialWidth = 800)
        {
            InitializeComponent();
            
            _viewModel = viewModel;
            _onDockBackRequested = onDockBackRequested;
            
            // Set the DataContext for data binding
            DataContext = _viewModel;
            
            // Set the initial window width to match the docked image area
            if (initialWidth > 0 && initialWidth >= MinWidth)
            {
                Width = initialWidth;
            }
            
            // Force fit mode for floating window
            if (_viewModel != null)
            {
                _viewModel.FitMode = true;
            }
            
            // Subscribe to closing event to dock back when window is closed
            Closing += FloatingImageWindow_Closing;
            
            // Subscribe to size changed event to recalculate fit
            SizeChanged += FloatingImageWindow_SizeChanged;
            
            // Subscribe to loaded event to fit image initially
            Loaded += FloatingImageWindow_Loaded;
        }

        /// <summary>
        /// Handle window closing - just allow the window to close without docking back
        /// </summary>
        private void FloatingImageWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Just allow the window to close
            // The user can use the re-dock button in the main window to restore the image area
        }
        
        /// <summary>
        /// Programmatically close this window (called from DockBack)
        /// </summary>
        public void CloseWindow()
        {
            Close();
        }
        
        /// <summary>
        /// Handle window loaded event
        /// </summary>
        private void FloatingImageWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Fit image to window when first loaded
            if (DisplayImage.Source != null)
            {
                UpdateLayout();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToScrollViewer();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
        
        /// <summary>
        /// Handle window size changed event
        /// </summary>
        private void FloatingImageWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Recalculate fit when window is resized, but skip when in full screen mode
            // (full screen mode size changes are handled by EnterFloatingFullScreen/ExitFullScreen)
            // Note: UpdateImage() will still fit images when changing files in full screen mode
            if (!_isFullScreen && _viewModel != null && DisplayImage.Source != null)
            {
                _viewModel.FitMode = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToScrollViewer();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Handle dock back button click
        /// </summary>
        private void DockBackButton_Click(object sender, RoutedEventArgs e)
        {
            // Exit full screen if needed
            if (_isFullScreen)
            {
                ExitFullScreen();
            }
            _onDockBackRequested?.Invoke();
        }

        /// <summary>
        /// Handle mouse wheel for zooming
        /// </summary>
        private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Safety check: ensure we have valid ViewModel and image loaded
            if (_viewModel == null || DisplayImage.Source == null) 
            {
                e.Handled = true;  // Mark event as handled to prevent default scrolling
                return;
            }
            
            // Always handle the mouse wheel event to prevent default ScrollViewer behavior
            // We want custom zoom behavior, not document-style scrolling
            e.Handled = true;
            
            if (_viewModel.FitMode)
            {
                // In Fit mode, mouse wheel should zoom in to exit fit mode
                if (e.Delta > 0)
                {
                    _viewModel.FitMode = false;
                    _viewModel.ZoomLevel = _viewModel.FitScale * 1.25;
                    CenterImageAfterZoom();
                }
                // Ignore zoom out in fit mode
                return;
            }
            
            // In zoom mode, use mouse wheel for zooming
            double oldZoom = _viewModel.ZoomLevel;
            if (e.Delta > 0)
            {
                // Zoom in
                _viewModel.ZoomLevel = Math.Min(_viewModel.ZoomLevel * 1.25, 5.0);
            }
            else
            {
                // Zoom out
                double newZoom = _viewModel.ZoomLevel / 1.25;
                
                // If we would zoom smaller than fit scale, go to fit mode instead
                if (newZoom <= _viewModel.FitScale)
                {
                    _viewModel.FitMode = true;
                    FitImageToScrollViewer();
                    return;
                }
                
                _viewModel.ZoomLevel = newZoom;
            }
            
            // Center the image after zoom change
            if (Math.Abs(_viewModel.ZoomLevel - oldZoom) > 0.0001)
            {
                CenterImageAfterZoom();
            }
        }

        /// <summary>
        /// Fit image to the scroll viewer - always fills the width, may clip height or show gray at bottom
        /// </summary>
        private void FitImageToScrollViewer()
        {
            if (_viewModel == null || DisplayImage.Source == null)
                return;

            if (DisplayImage.Source is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                double viewportW = ImageScrollViewer.ActualWidth;
                double viewportH = ImageScrollViewer.ActualHeight;

                if (viewportW < 10 || viewportH < 10) return;

                // Always use width to fill the screen/window
                // This may result in clipping at top/bottom if image is taller than viewport
                // or gray space at bottom if image is shorter than viewport
                double scale = viewportW / bmp.PixelWidth;

                _viewModel.FitScale = scale;
                _viewModel.ZoomLevel = scale;

                ImageScrollViewer.ScrollToHorizontalOffset(0);
                ImageScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        /// <summary>
        /// Center image in scroll viewer after zoom
        /// </summary>
        private void CenterImageAfterZoom()
        {
            if (_viewModel == null || DisplayImage.Source == null)
                return;

            if (_viewModel.FitMode)
            {
                // In fit mode, always reset to origin
                ImageScrollViewer.ScrollToHorizontalOffset(0);
                ImageScrollViewer.ScrollToVerticalOffset(0);
                return;
            }

            // In zoom mode, center the image
            if (DisplayImage.Source is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                double imgW = bmp.PixelWidth * _viewModel.ZoomLevel;
                double imgH = bmp.PixelHeight * _viewModel.ZoomLevel;
                double viewportW = ImageScrollViewer.ActualWidth;
                double viewportH = ImageScrollViewer.ActualHeight;
                
                if (viewportW < 10 || viewportH < 10) return;
                
                // Calculate center position
                double offsetX = Math.Max(0, (imgW - viewportW) / 2);
                double offsetY = Math.Max(0, (imgH - viewportH) / 2);
                
                ImageScrollViewer.ScrollToHorizontalOffset(offsetX);
                ImageScrollViewer.ScrollToVerticalOffset(offsetY);
            }
        }

        /// <summary>
        /// Handle mouse button down for drag-to-pan
        /// </summary>
        private void DisplayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only allow panning in manual zoom mode (not in fit mode)
            if (_viewModel == null || _viewModel.FitMode) return;
            
            // Record the starting mouse position relative to the ScrollViewer
            _lastMousePos = e.GetPosition(ImageScrollViewer);
            
            // Capture mouse input so we receive events even if cursor leaves the image
            DisplayImage.CaptureMouse();
        }

        /// <summary>
        /// Handle mouse button up to end drag-to-pan
        /// </summary>
        private void DisplayImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Clear the last mouse position to indicate panning has ended
            _lastMousePos = null;
            
            // Release mouse capture so normal mouse handling resumes
            DisplayImage.ReleaseMouseCapture();
        }

        /// <summary>
        /// Handle mouse move for drag-to-pan
        /// </summary>
        private void DisplayImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Only process mouse movement in manual zoom mode
            if (_viewModel == null || _viewModel.FitMode) return;

            // Only pan if we're tracking a drag operation
            if (_lastMousePos.HasValue && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Get current mouse position relative to the ScrollViewer
                var pos = e.GetPosition(ImageScrollViewer);
                
                // Calculate how far the mouse has moved since last update
                double dx = pos.X - _lastMousePos.Value.X;
                double dy = pos.Y - _lastMousePos.Value.Y;
                
                // Adjust scroll position by the opposite amount
                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - dx);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - dy);
                
                // Update last position for next movement calculation
                _lastMousePos = pos;
            }
        }

        /// <summary>
        /// Update the displayed image. Called by MainWindow when a new image is loaded.
        /// </summary>
        public void UpdateImage(System.Windows.Media.Imaging.BitmapSource? imageSource)
        {
            if (imageSource == null)
            {
                DisplayImage.Source = null;
                ImageScrollViewer.Visibility = Visibility.Collapsed;
                PlaceholderText.Visibility = Visibility.Visible;
            }
            else
            {
                // Capture zoom state before loading the new image
                bool previousFitMode = _viewModel?.FitMode ?? true;
                double previousZoomLevel = _viewModel?.ZoomLevel ?? 1.0;
                double previousFitScale = _viewModel?.FitScale ?? 1.0;

                DisplayImage.Source = imageSource;
                ImageScrollViewer.Visibility = Visibility.Visible;
                PlaceholderText.Visibility = Visibility.Collapsed;

                if (_viewModel != null)
                {
                    UpdateLayout();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (previousFitMode)
                        {
                            _viewModel.FitMode = true;
                            FitImageToScrollViewer();
                        }
                        else
                        {
                            // Recalculate fit scale for the new image, then restore zoom proportionally
                            FitImageToScrollViewer();  // sets FitScale
                            double newFitScale = _viewModel.FitScale;
                            double restoredZoom = (previousFitScale > 0.0001)
                                ? previousZoomLevel * (newFitScale / previousFitScale)
                                : previousZoomLevel;
                            restoredZoom = Math.Max(0.01, Math.Min(5.0, restoredZoom));
                            _viewModel.FitMode = false;
                            _viewModel.ZoomLevel = restoredZoom;
                            CenterImageAfterZoom();
                        }
                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
            }
        }
        
        /// <summary>
        /// Toggle full screen mode for this floating window
        /// </summary>
        private void ToggleFullScreen()
        {
            if (_isFullScreen)
            {
                ExitFullScreen();
            }
            else
            {
                EnterFloatingFullScreen();
            }
        }
        
        /// <summary>
        /// Enter full screen mode for the floating window
        /// </summary>
        private void EnterFloatingFullScreen()
        {
            // Save current state
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousLeft = Left;
            _previousTop = Top;
            _previousWidth = Width;
            _previousHeight = Height;
            
            // Enter full screen
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
            
            // Ensure toolbar controls remain visible in full screen
            ImageControlsGrid.Visibility = Visibility.Visible;
            
            // Force fit mode and recalculate fit for full screen
            if (_viewModel != null && DisplayImage.Source != null)
            {
                _viewModel.FitMode = true;
                
                // Force layout update to ensure window dimensions are current
                UpdateLayout();
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToScrollViewer();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
        
        /// <summary>
        /// Exit full screen mode for the floating window
        /// </summary>
        private void ExitFullScreen()
        {
            // Restore previous state
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            
            if (_previousWindowState == WindowState.Normal)
            {
                Left = _previousLeft;
                Top = _previousTop;
                Width = _previousWidth;
                Height = _previousHeight;
            }
            
            _isFullScreen = false;
            
            // Recalculate fit for restored window size
            if (_viewModel != null && DisplayImage.Source != null)
            {
                _viewModel.FitMode = true;
                
                // Force layout update to ensure window dimensions are current
                UpdateLayout();
                
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToScrollViewer();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
        
        /// <summary>
        /// Handle full screen button click
        /// </summary>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }
        
        /// <summary>
        /// Handle key down to intercept F11 for full screen
        /// </summary>
        private void FloatingImageWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }
    }
}
