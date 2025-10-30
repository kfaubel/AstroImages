
using AstroImages.Wpf.Services;
using AstroImages.Wpf.Converters;
using System.Windows.Controls;
using AstroImages.Wpf.ViewModels;
using System.Windows;
using System.Windows.Data;

namespace AstroImages.Wpf.Services
{
    public class ListViewColumnService : IListViewColumnService
    {
        private readonly System.Windows.Controls.ListView _fileListView;
        private readonly AppConfig _appConfig;
        private readonly System.Windows.Media.Brush _greenTextBrush;
        private readonly System.Windows.Media.Brush _blueTextBrush;

        public ListViewColumnService(System.Windows.Controls.ListView fileListView, AppConfig appConfig, System.Windows.Media.Brush greenTextBrush, System.Windows.Media.Brush blueTextBrush)
        {
            _fileListView = fileListView;
            _appConfig = appConfig;
            _greenTextBrush = greenTextBrush;
            _blueTextBrush = blueTextBrush;
        }

        public void UpdateListViewColumns()
        {
            // Prevent recursive or excessive column updates (not needed if only called from VM, but keep for safety)
            // Clear all columns and rebuild from scratch
            if (!(_fileListView.View is GridView gridView)) return;
            gridView.Columns.Clear();

            // Calculate optimal column widths based on content
            var columnWidths = CalculateOptimalColumnWidths();


            // Checkbox column (IsSelected)
            var checkFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.CheckBox));
            checkFactory.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected") { Mode = System.Windows.Data.BindingMode.TwoWay });
            checkFactory.SetValue(System.Windows.Controls.CheckBox.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            var checkTemplate = new DataTemplate { VisualTree = checkFactory };
            var checkboxColumn = new GridViewColumn
            {
                Header = "",
                Width = 30,
                CellTemplate = checkTemplate
            };
            gridView.Columns.Add(checkboxColumn);

            // Metadata button column with info icon (no gray box)
            var buttonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
            buttonFactory.SetValue(System.Windows.Controls.Button.ContentProperty, "ⓘ");
            buttonFactory.SetValue(System.Windows.Controls.Button.WidthProperty, 25.0);
            buttonFactory.SetValue(System.Windows.Controls.Button.HeightProperty, 25.0);
            buttonFactory.SetValue(System.Windows.Controls.Button.ToolTipProperty, "Show file metadata and headers");
            buttonFactory.SetValue(System.Windows.Controls.Button.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            buttonFactory.SetValue(System.Windows.Controls.Button.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
            buttonFactory.SetValue(System.Windows.Controls.Button.FontSizeProperty, 14.0);
            buttonFactory.SetValue(System.Windows.Controls.Button.FontWeightProperty, System.Windows.FontWeights.Bold);
            buttonFactory.SetValue(System.Windows.Controls.Button.ForegroundProperty, System.Windows.Media.Brushes.DarkBlue);
            buttonFactory.SetValue(System.Windows.Controls.Button.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            buttonFactory.SetValue(System.Windows.Controls.Button.BorderBrushProperty, System.Windows.Media.Brushes.Transparent);
            buttonFactory.SetValue(System.Windows.Controls.Button.BorderThicknessProperty, new System.Windows.Thickness(0));
            buttonFactory.SetValue(System.Windows.Controls.Button.PaddingProperty, new System.Windows.Thickness(2));
            
            // Bind the command to the ViewModel's ShowFileMetadataCommand
            // We need to find the MainWindowViewModel from the DataContext
            var commandBinding = new System.Windows.Data.Binding("DataContext.ShowFileMetadataCommand");
            commandBinding.RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.ListView), 1);
            buttonFactory.SetBinding(System.Windows.Controls.Button.CommandProperty, commandBinding);
            
            // Pass the current FileItem as the command parameter
            buttonFactory.SetBinding(System.Windows.Controls.Button.CommandParameterProperty, new System.Windows.Data.Binding("."));
            
            var buttonTemplate = new DataTemplate { VisualTree = buttonFactory };
            var metadataColumn = new GridViewColumn
            {
                Header = "",
                Width = 35,
                CellTemplate = buttonTemplate
            };
            gridView.Columns.Add(metadataColumn);

            // File column
            var fileColumn = new GridViewColumn
            {
                Header = "File",
                Width = columnWidths.ContainsKey("File") ? columnWidths["File"] : 150,
                DisplayMemberBinding = new System.Windows.Data.Binding("Name")
            };
            gridView.Columns.Add(fileColumn);

            // Size column (with converter)
            if (_appConfig.ShowSizeColumn)
            {
                var sizeColumn = new GridViewColumn
                {
                    Header = "Size",
                    Width = columnWidths.ContainsKey("Size") ? columnWidths["Size"] : 80,
                    DisplayMemberBinding = new System.Windows.Data.Binding("Size") { Converter = new AstroImages.Wpf.Converters.FileSizeConverter() }
                };
                gridView.Columns.Add(sizeColumn);
            }

            // Custom keyword columns (green)
            foreach (var keyword in _appConfig.CustomKeywords)
            {
                var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                
                // Use SafeDictionaryConverter with parameter to avoid KeyNotFoundException
                var binding = new System.Windows.Data.Binding("CustomKeywords");
                binding.Converter = new SafeDictionaryConverter();
                binding.ConverterParameter = keyword;
                
                factory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, binding);
                factory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, _greenTextBrush);
                factory.SetValue(System.Windows.Controls.TextBlock.TextTrimmingProperty, System.Windows.TextTrimming.CharacterEllipsis);
                var template = new DataTemplate { VisualTree = factory };
                var column = new GridViewColumn
                {
                    Header = keyword,
                    Width = columnWidths.ContainsKey($"Custom_{keyword}") ? columnWidths[$"Custom_{keyword}"] : 80,
                    CellTemplate = template
                };
                gridView.Columns.Add(column);
            }

            // FITS keyword columns (blue)
            foreach (var keyword in _appConfig.FitsKeywords)
            {
                var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                
                // Use SafeDictionaryConverter with parameter to avoid KeyNotFoundException
                var binding = new System.Windows.Data.Binding("FitsKeywords");
                binding.Converter = new SafeDictionaryConverter();
                binding.ConverterParameter = keyword;
                
                factory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, binding);
                factory.SetValue(System.Windows.Controls.TextBlock.ForegroundProperty, _blueTextBrush);
                factory.SetValue(System.Windows.Controls.TextBlock.TextTrimmingProperty, System.Windows.TextTrimming.CharacterEllipsis);
                var template = new DataTemplate { VisualTree = factory };
                var column = new GridViewColumn
                {
                    Header = keyword,
                    Width = columnWidths.ContainsKey($"FITS_{keyword}") ? columnWidths[$"FITS_{keyword}"] : 80,
                    CellTemplate = template
                };
                gridView.Columns.Add(column);
            }

            // Auto-resize all columns to fit content after a small delay
            // This ensures the ListView has been rendered with data before measuring
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                AutoResizeAllColumns();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Calculates optimal column widths based on content and headers.
        /// Returns a dictionary with column identifiers as keys and optimal widths as values.
        /// </summary>
        private Dictionary<string, double> CalculateOptimalColumnWidths()
        {
            var widths = new Dictionary<string, double>();
            
            // For now, return default widths that will be adjusted by AutoResizeAllColumns
            // This method could be enhanced to pre-calculate based on data analysis
            widths["File"] = 200;      // Start with reasonable default for filename column
            widths["Size"] = 80;       // Size column is typically narrow
            
            // Custom and FITS keyword columns start with minimum widths
            foreach (var keyword in _appConfig.CustomKeywords)
            {
                widths[$"Custom_{keyword}"] = Math.Max(60, keyword.Length * 8 + 20); // Header width + padding
            }
            
            foreach (var keyword in _appConfig.FitsKeywords)
            {
                widths[$"FITS_{keyword}"] = Math.Max(60, keyword.Length * 8 + 20); // Header width + padding
            }
            
            return widths;
        }

        /// <summary>
        /// Public method to trigger column auto-resizing.
        /// Can be called from external code when data changes.
        /// </summary>
        public void AutoResizeColumns()
        {
            // Use a small delay to ensure the ListView has been updated with new data
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                AutoResizeAllColumns();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Automatically resizes all columns to fit their content.
        /// This method measures the actual content and adjusts column widths accordingly.
        /// </summary>
        private void AutoResizeAllColumns()
        {
            if (!(_fileListView.View is GridView gridView)) return;
            
            try
            {
                // Measure and resize each column
                foreach (var column in gridView.Columns)
                {
                    if (column != null)
                    {
                        // Store original width
                        var originalWidth = column.Width;
                        
                        // Set to NaN to auto-size based on content
                        column.Width = double.NaN;
                        
                        // Force layout update
                        _fileListView.UpdateLayout();
                        
                        // Get the auto-calculated width
                        var autoWidth = double.IsNaN(column.ActualWidth) ? originalWidth : column.ActualWidth;
                        
                        // Apply minimum width constraints and some padding
                        var minWidth = GetMinimumColumnWidth(column);
                        var finalWidth = Math.Max(minWidth, autoWidth + 10); // Add 10px padding
                        
                        // Set the final calculated width
                        column.Width = finalWidth;
                    }
                }
            }
            catch (Exception ex)
            {
                // If auto-resizing fails, log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error auto-resizing columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the minimum width for a specific column based on its type.
        /// </summary>
        private double GetMinimumColumnWidth(GridViewColumn column)
        {
            // Checkbox and button columns should be narrow
            if (string.IsNullOrEmpty(column.Header?.ToString()))
            {
                // Determine by width: checkbox=30, metadata button=35
                return column.Width < 32 ? 30 : 35;
            }
            
            var header = column.Header?.ToString() ?? "";
            
            // Different minimum widths based on column type
            switch (header.ToLowerInvariant())
            {
                case "file":
                    return 120; // Filename column needs more space
                case "size":
                    return 60;  // Size column can be narrow
                default:
                    return Math.Max(50, header.Length * 7); // Dynamic based on header length
            }
        }
    }
}
