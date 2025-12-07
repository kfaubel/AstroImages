using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AstroImages.Wpf.Models;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Full-screen image viewer window
    /// </summary>
    public partial class FullScreenWindow : Window
    {
        private readonly ObservableCollection<FileItem> _files;
        private readonly AppConfig _appConfig;
        private int _currentIndex;
        private double _zoomLevel = 1.0;
        private System.Windows.Point? _lastMousePos;
        private bool _isPanning = false;

        public int CurrentIndex => _currentIndex;

        public FullScreenWindow(ObservableCollection<FileItem> files, int startIndex, AppConfig appConfig)
        {
            InitializeComponent();
            
            _files = files;
            _currentIndex = startIndex;
            _appConfig = appConfig;

            // Set cursor to hand
            Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Show help dialog if enabled
            if (_appConfig.ShowFullScreenHelp)
            {
                var helpDialog = new FullScreenHelpDialog(_appConfig);
                helpDialog.Owner = this;
                helpDialog.ShowDialog();
            }

            // Load the initial image
            LoadCurrentImage();
        }

        private void LoadCurrentImage()
        {
            if (_currentIndex < 0 || _currentIndex >= _files.Count)
                return;

            var fileItem = _files[_currentIndex];
            
            try
            {
                var image = FitsImageRenderer.RenderFitsFile(fileItem.Path, autoStretch: true, stretchAggressiveness: _appConfig.StretchAggressiveness);
                DisplayImage.Source = image;
                
                // Update info text with selection status
                UpdateInfoText();
                
                // Reset zoom to fit
                FitImageToWindow();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateInfoText()
        {
            if (_currentIndex >= 0 && _currentIndex < _files.Count)
            {
                var fileItem = _files[_currentIndex];
                int zoomPercentage = (int)(_zoomLevel * 100);
                InfoTextBlock.Text = $"{fileItem.Name} ({_currentIndex + 1}/{_files.Count}) {zoomPercentage}%";
                
                // Set color based on selection status
                InfoTextBlock.Foreground = fileItem.IsSelected 
                    ? System.Windows.Media.Brushes.Red 
                    : System.Windows.Media.Brushes.White;
            }
        }

        private void FitImageToWindow()
        {
            if (DisplayImage.Source == null)
                return;

            var bmp = DisplayImage.Source as BitmapSource;
            if (bmp == null)
                return;

            double imageWidth = bmp.PixelWidth;
            double imageHeight = bmp.PixelHeight;
            
            double viewportWidth = ImageScrollViewer.ActualWidth;
            double viewportHeight = ImageScrollViewer.ActualHeight;

            if (viewportWidth <= 0 || viewportHeight <= 0)
            {
                viewportWidth = ActualWidth;
                viewportHeight = ActualHeight;
            }

            double scaleX = viewportWidth / imageWidth;
            double scaleY = viewportHeight / imageHeight;
            
            _zoomLevel = Math.Min(scaleX, scaleY);
            _zoomLevel = Math.Min(_zoomLevel, 1.0); // Don't zoom in beyond 100%
            
            ScaleTransform.ScaleX = _zoomLevel;
            ScaleTransform.ScaleY = _zoomLevel;
            
            // Center the image
            ImageScrollViewer.ScrollToHorizontalOffset(0);
            ImageScrollViewer.ScrollToVerticalOffset(0);
            
            // Update info text to show new zoom level
            UpdateInfoText();
        }

        private void ZoomIn()
        {
            // Get current center position before zoom
            double centerX = ImageScrollViewer.HorizontalOffset + ImageScrollViewer.ViewportWidth / 2;
            double centerY = ImageScrollViewer.VerticalOffset + ImageScrollViewer.ViewportHeight / 2;
            
            // Calculate position as ratio of current content size
            double ratioX = centerX / ImageScrollViewer.ExtentWidth;
            double ratioY = centerY / ImageScrollViewer.ExtentHeight;
            
            _zoomLevel *= 1.2;
            _zoomLevel = Math.Min(_zoomLevel, 10.0); // Max 10x zoom
            ScaleTransform.ScaleX = _zoomLevel;
            ScaleTransform.ScaleY = _zoomLevel;
            
            // Update scroll position to maintain center point
            ImageScrollViewer.UpdateLayout();
            ImageScrollViewer.ScrollToHorizontalOffset(ratioX * ImageScrollViewer.ExtentWidth - ImageScrollViewer.ViewportWidth / 2);
            ImageScrollViewer.ScrollToVerticalOffset(ratioY * ImageScrollViewer.ExtentHeight - ImageScrollViewer.ViewportHeight / 2);
            
            // Update info text to show new zoom level
            UpdateInfoText();
        }

        private void ZoomOut()
        {
            // Get current center position before zoom
            double centerX = ImageScrollViewer.HorizontalOffset + ImageScrollViewer.ViewportWidth / 2;
            double centerY = ImageScrollViewer.VerticalOffset + ImageScrollViewer.ViewportHeight / 2;
            
            // Calculate position as ratio of current content size
            double ratioX = centerX / ImageScrollViewer.ExtentWidth;
            double ratioY = centerY / ImageScrollViewer.ExtentHeight;
            
            _zoomLevel /= 1.2;
            _zoomLevel = Math.Max(_zoomLevel, 0.1); // Min 0.1x zoom
            ScaleTransform.ScaleX = _zoomLevel;
            ScaleTransform.ScaleY = _zoomLevel;
            
            // Update scroll position to maintain center point
            ImageScrollViewer.UpdateLayout();
            ImageScrollViewer.ScrollToHorizontalOffset(ratioX * ImageScrollViewer.ExtentWidth - ImageScrollViewer.ViewportWidth / 2);
            ImageScrollViewer.ScrollToVerticalOffset(ratioY * ImageScrollViewer.ExtentHeight - ImageScrollViewer.ViewportHeight / 2);
            
            // Update info text to show new zoom level
            UpdateInfoText();
        }

        private void GoToNext()
        {
            if (_files.Count == 0)
                return;
                
            _currentIndex++;
            if (_currentIndex >= _files.Count)
            {
                _currentIndex = 0; // Loop back to start
            }
            LoadCurrentImage();
        }

        private void GoToPrevious()
        {
            if (_files.Count == 0)
                return;
                
            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _files.Count - 1; // Loop to end
            }
            LoadCurrentImage();
        }

        private void ToggleSelection()
        {
            if (_currentIndex >= 0 && _currentIndex < _files.Count)
            {
                _files[_currentIndex].IsSelected = !_files[_currentIndex].IsSelected;
                // Update info text to show selection status
                UpdateInfoText();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Right:
                    GoToNext();
                    break;
                case Key.Left:
                    GoToPrevious();
                    break;
                case Key.Space:
                    ToggleSelection();
                    break;
                case Key.Up:
                    ZoomIn();
                    break;
                case Key.Down:
                    ZoomOut();
                    break;
                case Key.F:
                    FitImageToWindow();
                    break;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
            
            // Prevent the ScrollViewer from handling the mouse wheel
            e.Handled = true;
        }

        private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Handle zoom
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
            
            // Prevent the ScrollViewer from scrolling
            e.Handled = true;
        }

        private void ImageScrollViewer_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Prevent ScrollViewer from handling arrow keys
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
            }
        }

        private void DisplayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(ImageScrollViewer);
            _isPanning = true;
            DisplayImage.CaptureMouse();
            Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void DisplayImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            DisplayImage.ReleaseMouseCapture();
            Cursor = System.Windows.Input.Cursors.Hand;
        }

        private void DisplayImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isPanning || !_lastMousePos.HasValue)
                return;

            var pos = e.GetPosition(ImageScrollViewer);
            var dx = pos.X - _lastMousePos.Value.X;
            var dy = pos.Y - _lastMousePos.Value.Y;

            ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - dx);
            ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - dy);

            _lastMousePos = pos;
        }
    }
}
