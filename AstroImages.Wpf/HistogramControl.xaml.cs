using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using AstroImages.Wpf.ViewModels;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfBrush = System.Windows.Media.Brush;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfPoint = System.Windows.Point;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Interaction logic for HistogramControl.xaml
    /// Displays a histogram visualization of image pixel values with support for linear and logarithmic scales
    /// </summary>
    public partial class HistogramControl : System.Windows.Controls.UserControl
    {
        private HistogramViewModel? _viewModel;

        public HistogramControl()
        {
            InitializeComponent();
            Loaded += HistogramControl_Loaded;
        }

        private void HistogramControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as HistogramViewModel;
            if (_viewModel != null)
            {
                _viewModel.HistogramDataChanged += OnHistogramDataChanged;
                DrawHistogram();
            }
        }

        private void OnHistogramDataChanged()
        {
            DrawHistogram();
        }

        private void HistogramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawHistogram();
        }

        private void DrawHistogram()
        {
            if (_viewModel == null || !_viewModel.HasHistogramData || HistogramCanvas.ActualWidth <= 0 || HistogramCanvas.ActualHeight <= 0)
            {
                return;
            }

            HistogramCanvas.Children.Clear();

            var redData = _viewModel.RedHistogram;
            var greenData = _viewModel.GreenHistogram;
            var blueData = _viewModel.BlueHistogram;
            var grayData = _viewModel.GrayHistogram;

            // Determine which histograms to draw
            bool isGrayscale = _viewModel.IsGrayscale;
            
            if (isGrayscale && grayData != null && grayData.Length > 0)
            {
                DrawChannel(grayData, WpfColors.White, _viewModel.IsLogarithmicScale);
            }
            else
            {
                // Draw RGB channels
                if (redData != null && redData.Length > 0)
                {
                    DrawChannel(redData, WpfColor.FromArgb(180, 255, 0, 0), _viewModel.IsLogarithmicScale);
                }
                if (greenData != null && greenData.Length > 0)
                {
                    DrawChannel(greenData, WpfColor.FromArgb(180, 0, 255, 0), _viewModel.IsLogarithmicScale);
                }
                if (blueData != null && blueData.Length > 0)
                {
                    DrawChannel(blueData, WpfColor.FromArgb(180, 0, 0, 255), _viewModel.IsLogarithmicScale);
                }
            }
        }

        private void DrawChannel(int[] data, WpfColor color, bool useLogScale)
        {
            if (data == null || data.Length == 0)
                return;

            double width = HistogramCanvas.ActualWidth;
            double height = HistogramCanvas.ActualHeight;

            // Find max value for scaling
            int maxValue = 0;
            double[] scaledData = new double[data.Length];
            
            if (useLogScale)
            {
                // Apply logarithmic scale
                for (int i = 0; i < data.Length; i++)
                {
                    scaledData[i] = data[i] > 0 ? Math.Log10(data[i] + 1) : 0;
                    if (scaledData[i] > maxValue)
                        maxValue = (int)scaledData[i];
                }
            }
            else
            {
                // Linear scale
                for (int i = 0; i < data.Length; i++)
                {
                    scaledData[i] = data[i];
                    if (data[i] > maxValue)
                        maxValue = data[i];
                }
            }

            if (maxValue == 0)
                return;

            // Create polyline for histogram
            var polyline = new Polyline
            {
                Stroke = new WpfSolidColorBrush(color),
                StrokeThickness = 1
            };

            // Build points for the histogram
            double binWidth = width / data.Length;
            
            // Start from bottom left
            polyline.Points.Add(new WpfPoint(0, height));
            
            // Add two points per bin (left and right edges) to create horizontal segments
            for (int i = 0; i < data.Length; i++)
            {
                double xLeft = i * binWidth;
                double xRight = (i + 1) * binWidth;
                double normalizedValue = scaledData[i] / maxValue;
                double y = height - (normalizedValue * height);
                
                // Add point at left edge of bin
                polyline.Points.Add(new WpfPoint(xLeft, y));
                // Add point at right edge of bin (same height)
                polyline.Points.Add(new WpfPoint(xRight, y));
            }
            
            // End at bottom right
            polyline.Points.Add(new WpfPoint(width, height));

            // Create filled polygon for better visualization
            var polygon = new Polygon
            {
                Fill = new WpfSolidColorBrush(WpfColor.FromArgb(60, color.R, color.G, color.B)),
                Stroke = new WpfSolidColorBrush(color),
                StrokeThickness = 1
            };

            foreach (var point in polyline.Points)
            {
                polygon.Points.Add(point);
            }

            HistogramCanvas.Children.Add(polygon);
        }
    }
}
