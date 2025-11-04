using System.Windows;
using System.Linq;
using AstroImages.Wpf.ViewModels;
using AstroImages.Wpf.Services;
using AstroImages.Wpf.Models;
using System.Windows.Media;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Main Window class - this is the primary application window that users interact with.
    /// Uses the MVVM (Model-View-ViewModel) pattern where:
    /// - View = MainWindow.xaml (the UI layout)
    /// - ViewModel = MainWindowViewModel (the logic and data)
    /// - Model = Various service classes and data models
    /// </summary>
    public partial class MainWindow : Window
    {
        // Private field to hold reference to the ViewModel
        // The ? means it can be null (nullable reference type)
        private MainWindowViewModel? _viewModel;

        // Private field to hold reference to the ListView column service
        // Used for auto-resizing columns when data changes
        private IListViewColumnService? _listViewColumnService;

        /// <summary>
        /// Constructor - called when creating a new MainWindow instance.
        /// This sets up all the services and connects the UI to the ViewModel.
        /// </summary>
        public MainWindow()
        {
            // Initialize WPF components from the XAML file
            // This must be called before accessing any UI elements
            InitializeComponent();

            // Load application configuration from settings file
            // AppConfig.Load() reads saved settings or creates defaults
            var config = AppConfig.Load();
            
            // Restore window position and size from saved configuration
            RestoreWindowState(config);

            // Create all the service instances that the application needs
            // Services handle specific functionality (file management, dialogs, etc.)
            
            // Handles file system operations (loading folders, file info)
            var fileManagementService = new FileManagementService();
            
            // Extracts metadata from filenames (RMS, HFR values, etc.)
            var keywordExtractionService = new KeywordExtractionService();
            
            // Shows folder selection dialog to user
            var folderDialogService = new FolderDialogService();
            
            // Shows general options/settings dialog
            var generalOptionsDialogService = new GeneralOptionsDialogService();
            
            // Shows custom keywords configuration dialog
            var customKeywordsDialogService = new CustomKeywordsDialogService();
            
            // Shows FITS keywords configuration dialog
            var fitsKeywordsDialogService = new FitsKeywordsDialogService();

            // Create colored brushes for styling the ListView columns from theme resources
            var greenBrush = (System.Windows.Application.Current.TryFindResource("ThemeAccentGreen") as System.Windows.Media.Brush) 
                ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 128, 0)); // Fallback
            var blueBrush = (System.Windows.Application.Current.TryFindResource("ThemeAccentBlue") as System.Windows.Media.Brush)
                ?? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 192)); // Fallback
            
            // Service that manages ListView columns (adding, removing, styling)
            // Needs reference to the actual ListView control from XAML
            // Store as field so we can call auto-resize methods later
            _listViewColumnService = new ListViewColumnService(FileListView, config, greenBrush, blueBrush);

            // Create the ViewModel with all required services
            // This uses "dependency injection" - passing dependencies to the constructor
            _viewModel = new MainWindowViewModel(
                fileManagementService,
                keywordExtractionService,
                config,
                folderDialogService,
                generalOptionsDialogService,
                customKeywordsDialogService,
                fitsKeywordsDialogService,
                _listViewColumnService
            );

            // Connect the ViewModel to the UI through DataContext
            // This enables data binding - UI elements can bind to ViewModel properties
            this.DataContext = _viewModel;

            // Connect the ListView to the Files collection in the ViewModel
            // ItemsSource tells the ListView where to get its data
            FileListView.ItemsSource = _viewModel.Files;

            // Initialize the ListView columns based on configuration
            _listViewColumnService.UpdateListViewColumns();
            
            // Auto-resize columns to fit content after initial setup
            _listViewColumnService.AutoResizeColumns();

            // Wire up fit request event
            _viewModel.FitRequested += () => FitImageToScrollViewer();
            _viewModel.ZoomActualRequested += () => CenterImageAfterZoom();
            
            // Wire up files loaded event to auto-resize columns
            _viewModel.FilesLoaded += () => _listViewColumnService?.AutoResizeColumns();
            
            // Wire up auto-stretch changed event to refresh current image
            _viewModel.AutoStretchChanged += () => UpdateImageDisplay();

            // Load last directory if available
            if (!string.IsNullOrEmpty(config.LastOpenDirectory) && System.IO.Directory.Exists(config.LastOpenDirectory))
            {
                _viewModel.LoadFilesCommand.Execute(config.LastOpenDirectory);
                // Select first image if available
                if (_viewModel.Files.Count > 0)
                {
                    _viewModel.SelectedIndex = 0;
                    // Don't set fit mode here - let UpdateImageDisplay handle it
                }
                
                // Auto-resize columns after loading initial files
                _listViewColumnService?.AutoResizeColumns();
            }

            // Wire up file selection to image display
            FileListView.SelectionChanged += FileListView_SelectionChanged;
            UpdateImageDisplay();

            // Handle pane resize for Fit mode
            ImageScrollViewer.SizeChanged += ImageScrollViewer_SizeChanged;
        }

        private void ImageScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_viewModel != null && _viewModel.FitMode && DisplayImage.Source != null)
            {
                // Use a timer to avoid too many rapid updates
                _fitTimer?.Stop();
                _fitTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                _fitTimer.Tick += (s, args) =>
                {
                    _fitTimer.Stop();
                    FitImageToScrollViewer();
                };
                _fitTimer.Start();
            }
        }

        private System.Windows.Threading.DispatcherTimer? _fitTimer;

        private void FileListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Show loading cursor when user selects a file
            this.Cursor = System.Windows.Input.Cursors.Wait;
            
            UpdateImageDisplay();
            if (_viewModel != null && _viewModel.FitMode)
                CenterImageInScrollViewer();
        }

        /// <summary>
        /// Updates the image display based on the currently selected file in the ListView.
        /// This method handles the complete process of displaying an image:
        /// 1. Validates that we have a valid selection
        /// 2. Loads and renders the image file (including FITS processing)
        /// 3. Sets up the zoom/fit mode
        /// 4. Handles error cases gracefully
        /// 
        /// This is a key method in the image viewing workflow and is called whenever
        /// the user selects a different file in the list.
        /// </summary>
        private void UpdateImageDisplay()
        {
            // Validate that we have a valid selection
            // Check for: null ViewModel, no selection (-1), or selection out of bounds
            if (_viewModel == null || _viewModel.SelectedIndex < 0 || _viewModel.SelectedIndex >= _viewModel.Files.Count)
            {
                // No valid selection - hide the image and show placeholder text
                DisplayImage.Source = null;                        // Clear any existing image
                ImageScrollViewer.Visibility = Visibility.Collapsed;  // Hide the image viewer
                PlaceholderText.Visibility = Visibility.Visible;      // Show "No image selected" text
                
                // Restore normal cursor since no image processing is needed
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                return; // Exit early since there's nothing to display
            }

            // Get the currently selected file from the ViewModel's Files collection
            var file = _viewModel.Files[_viewModel.SelectedIndex];
            
            // Verify the file still exists on disk (user might have moved/deleted it)
            if (System.IO.File.Exists(file.Path))
            {
                // Use our custom FITS renderer to load and process the image
                // This handles both FITS files and standard image formats (JPEG, PNG, etc.)
                var image = FitsImageRenderer.RenderFitsFile(file.Path, _viewModel.AutoStretch);
                
                if (image != null)
                {
                    // Successfully loaded the image - now display it
                    
                    // Set the image source to our loaded bitmap
                    DisplayImage.Source = image;
                    
                    // Show the image immediately
                    ImageScrollViewer.Visibility = Visibility.Visible;
                    PlaceholderText.Visibility = Visibility.Collapsed;

                    // Always start in Fit mode for new images
                    _viewModel.FitMode = true;

                    // Force layout update to ensure ScrollViewer is properly sized
                    this.UpdateLayout();
                    
                    // Use a small delay to ensure the UI is fully rendered before calculating fit scale
                    // This helps avoid timing issues where the ScrollViewer might not be sized yet
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FitImageToScrollViewer();
                        System.Diagnostics.Debug.WriteLine("Image loaded and fit mode applied");
                        
                        // Restore normal cursor after image rendering is complete
                        this.Cursor = System.Windows.Input.Cursors.Arrow;
                        
                        // Notify the ViewModel that image rendering is complete
                        _viewModel?.OnImageRenderingCompleted();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                    
                    return;
                    
                }
            }
            
            // If failed to load
            DisplayImage.Source = null;
            ImageScrollViewer.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Visible;
            
            // Restore normal cursor after failed image loading
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void CalculateAndApplyFitScale()
        {
            if (_viewModel == null || DisplayImage.Source == null)
                return;

            var bmp = DisplayImage.Source as System.Windows.Media.Imaging.BitmapSource;
            if (bmp == null)
                return;

            // Get current viewport size - use the parent grid if ScrollViewer isn't sized yet
            double viewportW = ImageScrollViewer.ActualWidth;
            double viewportH = ImageScrollViewer.ActualHeight;
            
            // DEBUG: Check what we're getting for viewport size
            System.Diagnostics.Debug.WriteLine($"ScrollViewer size: {viewportW}x{viewportH}");
            
            // If ScrollViewer isn't sized, try to get from parent container
            if (viewportW < 10 || viewportH < 10)
            {
                var parent = ImageScrollViewer.Parent as FrameworkElement;
                if (parent != null)
                {
                    viewportW = parent.ActualWidth - 40; // Account for margins/borders
                    viewportH = parent.ActualHeight - 40;
                    System.Diagnostics.Debug.WriteLine($"Parent size: {viewportW}x{viewportH}");
                }
            }
            
            // Still no good size? Use reasonable defaults based on window
            if (viewportW < 10 || viewportH < 10)
            {
                viewportW = this.ActualWidth * 0.6; // Approximate main content area
                viewportH = this.ActualHeight * 0.8;
                System.Diagnostics.Debug.WriteLine($"Window-based size: {viewportW}x{viewportH}");
            }

            double imgW = bmp.PixelWidth;
            double imgH = bmp.PixelHeight;
            
            System.Diagnostics.Debug.WriteLine($"Image size: {imgW}x{imgH}");
            
            if (imgW > 0 && imgH > 0 && viewportW > 0 && viewportH > 0)
            {
                // Calculate fit scale with some padding
                double scaleX = (viewportW - 20) / imgW;
                double scaleY = (viewportH - 20) / imgH;
                double fitScale = Math.Min(scaleX, scaleY);
                
                System.Diagnostics.Debug.WriteLine($"Calculated fit scale: {fitScale}");
                
                // Ensure reasonable bounds - but if it's still too small, use a fallback
                fitScale = Math.Max(0.01, Math.Min(5.0, fitScale));
                
                // If fit scale is still very small (less than 1%), something is wrong - use 10% as fallback
                if (fitScale < 0.01)
                {
                    fitScale = 0.1;
                    System.Diagnostics.Debug.WriteLine($"Using fallback fit scale: {fitScale}");
                }
                
                _viewModel.FitScale = fitScale;
                _viewModel.ZoomLevel = fitScale;
                
                System.Diagnostics.Debug.WriteLine($"Final zoom level set to: {_viewModel.ZoomLevel}");
            }
            else
            {
                // Fallback if we can't calculate properly
                System.Diagnostics.Debug.WriteLine("Cannot calculate fit scale - using fallback 0.1");
                _viewModel.FitScale = 0.1;
                _viewModel.ZoomLevel = 0.1;
            }
        }

        // Fit image to ScrollViewer viewport by adjusting ZoomLevel
        private void FitImageToScrollViewer()
        {
            CalculateAndApplyFitScale();
            
            // Reset scroll to origin for fit mode
            ImageScrollViewer.ScrollToHorizontalOffset(0);
            ImageScrollViewer.ScrollToVerticalOffset(0);
        }

        // Mouse pan state tracking for drag-to-pan functionality
        // Nullable Point - null means no panning in progress, non-null stores last mouse position
        private System.Windows.Point? _lastMousePos = null;

        /// <summary>
        /// Event handler for mouse wheel scrolling over the image viewer.
        /// Implements custom zoom behavior that works differently in Fit mode vs Manual zoom mode.
        /// 
        /// Behavior in Fit Mode:
        /// - Mouse wheel up: Exit fit mode and zoom in slightly
        /// - Mouse wheel down: Ignored (stay in fit mode)
        /// 
        /// Behavior in Manual Zoom Mode:
        /// - Mouse wheel up: Zoom in (up to 500% maximum)
        /// - Mouse wheel down: Zoom out (if would go below fit scale, return to fit mode)
        /// 
        /// This provides intuitive zoom behavior where fit mode is the "default" state
        /// and manual zoom is entered/exited naturally through mouse wheel interaction.
        /// </summary>
        /// <param name="sender">The ImageScrollViewer control</param>
        /// <param name="e">Mouse wheel event arguments containing scroll direction and amount</param>
        private void ImageScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
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

        // Center image in ScrollViewer after zoom or fit
        private void CenterImageInScrollViewer()
        {
            CenterImageAfterZoom();
        }

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
        /// Event handler for mouse button press on the image.
        /// Initiates drag-to-pan functionality when in manual zoom mode.
        /// 
        /// Drag-to-pan only works in manual zoom mode - in fit mode, the entire image
        /// is visible so panning doesn't make sense.
        /// 
        /// Mouse capture ensures we continue to receive mouse events even if the
        /// cursor moves outside the image bounds during dragging.
        /// </summary>
        /// <param name="sender">The DisplayImage control</param>
        /// <param name="e">Mouse button event arguments</param>
        private void DisplayImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only allow panning in manual zoom mode (not in fit mode)
            if (_viewModel == null || _viewModel.FitMode) return;
            
            // Record the starting mouse position relative to the ScrollViewer
            // This will be used to calculate movement delta in MouseMove
            _lastMousePos = e.GetPosition(ImageScrollViewer);
            
            // Capture mouse input so we receive events even if cursor leaves the image
            DisplayImage.CaptureMouse();
        }

        /// <summary>
        /// Event handler for mouse button release on the image.
        /// Ends the drag-to-pan operation by clearing the tracking state and releasing mouse capture.
        /// 
        /// This method is called when the user releases the left mouse button, regardless of
        /// where the cursor is located (thanks to mouse capture).
        /// </summary>
        /// <param name="sender">The DisplayImage control</param>
        /// <param name="e">Mouse button event arguments</param>
        private void DisplayImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Clear the last mouse position to indicate panning has ended
            _lastMousePos = null;
            
            // Release mouse capture so normal mouse handling resumes
            DisplayImage.ReleaseMouseCapture();
        }

        /// <summary>
        /// Event handler for mouse movement over the image.
        /// Implements the actual panning logic by calculating mouse movement delta
        /// and adjusting the ScrollViewer's scroll position accordingly.
        /// 
        /// The panning algorithm:
        /// 1. Calculate how far the mouse has moved since the last position
        /// 2. Adjust the ScrollViewer's scroll offset by the opposite amount
        /// 3. Update the last mouse position for the next movement calculation
        /// 
        /// Moving the mouse right should scroll the content left (revealing more content on the right),
        /// hence the negative delta in the scroll offset calculation.
        /// </summary>
        /// <param name="sender">The DisplayImage control</param>
        /// <param name="e">Mouse movement event arguments</param>
        private void DisplayImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Only process mouse movement in manual zoom mode
            if (_viewModel == null || _viewModel.FitMode) return;

            // Only pan if we're tracking a drag operation (left button is pressed and we have a start position)
            if (_lastMousePos.HasValue && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Get current mouse position relative to the ScrollViewer
                var pos = e.GetPosition(ImageScrollViewer);
                
                // Calculate how far the mouse has moved since last update
                double dx = pos.X - _lastMousePos.Value.X;  // Horizontal movement
                double dy = pos.Y - _lastMousePos.Value.Y;  // Vertical movement
                
                // Adjust scroll position by the opposite amount (pan in opposite direction of mouse movement)
                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - dx);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - dy);
                
                // Update last position for next movement calculation
                _lastMousePos = pos;
            }
        }

        /// <summary>
        /// Event handler for Help > Documentation menu item.
        /// Shows the documentation window with README content and help information.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Event arguments for the click event</param>
        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            // Create a new documentation window instance
            var documentationWindow = new DocumentationWindow();
            
            // Set this main window as the owner - makes dialog appear on top and centered
            documentationWindow.Owner = this;
            
            // Show as modal dialog - user must close it before returning to main window
            documentationWindow.ShowDialog();
        }

        /// <summary>
        /// Event handler for Help > About menu item.
        /// Reuses the splash screen window to show application information.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Event arguments for the click event</param>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            // Reuse the splash screen window as an "About" dialog
            var aboutWindow = new SplashWindow();
            
            // Set this main window as the owner
            aboutWindow.Owner = this;
            
            // Change the title to indicate this is the About dialog
            aboutWindow.Title = "About AstroImages";
            
            // Show as modal dialog
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// Event handler for Help > Check for Updates menu item.
        /// Manually checks for updates from GitHub releases.
        /// </summary>
        /// <param name="sender">The menu item that was clicked</param>
        /// <param name="e">Event arguments for the click event</param>
        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show working cursor
                this.Cursor = System.Windows.Input.Cursors.Wait;
                
                var config = AppConfig.Load();
                
                // Create update service
                using var updateService = new Services.UpdateService(config.UpdateRepoOwner, config.UpdateRepoName);
                
                // Check for updates
                var updateInfo = await updateService.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    // Update available - show dialog
                    var updateDialog = new UpdateDialog(updateInfo, updateService);
                    updateDialog.Owner = this;
                    
                    var result = updateDialog.ShowDialog();
                    
                    // Update config if user changed preferences
                    if (updateDialog.DisableUpdateChecks)
                    {
                        config.CheckForUpdates = false;
                        config.Save();
                    }
                }
                else
                {
                    // No update available - inform user
                    System.Windows.MessageBox.Show(
                        "You are running the latest version of AstroImages.",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                System.Windows.MessageBox.Show(
                    $"Unable to check for updates: {ex.Message}",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                // Restore normal cursor
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        #region Window State Management
        /// <summary>
        /// Restores the window position, size, and state from the saved configuration.
        /// Called during window initialization to restore the user's preferred layout.
        /// </summary>
        /// <param name="config">Application configuration containing saved window state</param>
        private void RestoreWindowState(AppConfig config)
        {
            // Restore window size
            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                this.Width = config.WindowWidth;
                this.Height = config.WindowHeight;
            }

            // Restore window position if valid (not NaN)
            if (!double.IsNaN(config.WindowLeft) && !double.IsNaN(config.WindowTop))
            {
                this.Left = config.WindowLeft;
                this.Top = config.WindowTop;
            }

            // Restore window state (Normal, Minimized, Maximized)
            this.WindowState = (WindowState)config.WindowState;

            // Subscribe to window events to save state when changed
            this.LocationChanged += MainWindow_LocationChanged;
            this.SizeChanged += MainWindow_SizeChanged;
            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;
        }

        /// <summary>
        /// Event handler for when the window position changes.
        /// Saves the new position to configuration.
        /// </summary>
        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            // Only save if window is in normal state (not minimized or maximized)
            if (this.WindowState == WindowState.Normal)
            {
                var config = AppConfig.Load();
                config.WindowLeft = this.Left;
                config.WindowTop = this.Top;
                config.Save();
            }
        }

        /// <summary>
        /// Event handler for when the window size changes.
        /// Saves the new size to configuration.
        /// </summary>
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only save if window is in normal state
            if (this.WindowState == WindowState.Normal)
            {
                var config = AppConfig.Load();
                config.WindowWidth = this.Width;
                config.WindowHeight = this.Height;
                config.Save();
            }
            
            // Update the File column width to be responsive to window width
            _listViewColumnService?.UpdateFileColumnWidth();
            
            // Adjust splitter position when window resizes (with small delay for layout)
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                _listViewColumnService?.AdjustSplitterForOptimalWidth();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Event handler for when the window state changes (Normal/Minimized/Maximized).
        /// Saves the new state to configuration.
        /// </summary>
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            var config = AppConfig.Load();
            config.WindowState = (int)this.WindowState;
            config.Save();
        }

        /// <summary>
        /// Event handler for when the window is closing.
        /// Saves the final window state to configuration.
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var config = AppConfig.Load();
            
            // Save current window state
            if (this.WindowState == WindowState.Normal)
            {
                config.WindowLeft = this.Left;
                config.WindowTop = this.Top;
                config.WindowWidth = this.Width;
                config.WindowHeight = this.Height;
            }
            config.WindowState = (int)this.WindowState;
            
            config.Save();
        }
        #endregion
    }
}


