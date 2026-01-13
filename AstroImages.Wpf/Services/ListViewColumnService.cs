
using AstroImages.Wpf.Services;
using AstroImages.Wpf.Converters;
using System.Windows.Controls;
using AstroImages.Wpf.ViewModels;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls.Primitives;

namespace AstroImages.Wpf.Services
{
    public class ListViewColumnService : IListViewColumnService
    {
        private readonly System.Windows.Controls.ListView _fileListView;
        private readonly AppConfig _appConfig;
        private GridSplitter? _gridSplitter; // Track the splitter for manual move detection

        public ListViewColumnService(System.Windows.Controls.ListView fileListView, AppConfig appConfig, System.Windows.Media.Brush greenTextBrush, System.Windows.Media.Brush blueTextBrush)
        {
            _fileListView = fileListView;
            _appConfig = appConfig;
            // Note: greenTextBrush and blueTextBrush parameters are no longer needed as we bind to theme resources directly
        }
        
        /// <summary>
        /// Creates a clickable column header that triggers sorting when clicked
        /// </summary>
        private GridViewColumnHeader CreateSortableHeader(string headerText)
        {
            var header = new GridViewColumnHeader
            {
                Content = headerText,
                Tag = headerText // Store column name for sorting
            };
            
            // Add click handler for sorting
            header.Click += ColumnHeader_Click;
            
            return header;
        }
        
        /// <summary>
        /// Handles column header clicks to trigger sorting
        /// </summary>
        private void ColumnHeader_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header && header.Tag is string columnName)
            {
                // Get the ViewModel from the ListView's DataContext
                if (_fileListView.DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SortByColumn(columnName);
                }
            }
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
            
            // Create a sortable header for the checkbox column
            var checkboxHeader = CreateSortableHeader("Mark");
            
            var checkboxColumn = new GridViewColumn
            {
                Header = checkboxHeader,
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
            // Use theme resource for button foreground color
            var buttonForeground = System.Windows.Application.Current.TryFindResource("ThemeAccentBlue") as System.Windows.Media.Brush
                ?? System.Windows.Media.Brushes.DarkBlue; // Fallback
            buttonFactory.SetValue(System.Windows.Controls.Button.ForegroundProperty, buttonForeground);
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
                Header = "Info",
                Width = 35,
                CellTemplate = buttonTemplate
            };
            gridView.Columns.Add(metadataColumn);

            // File column - always use fresh width calculation (no saved widths)
            var fileColumn = new GridViewColumn
            {
                Header = CreateSortableHeader("File"),
                Width = GetFileColumnWidth(), // Always calculate fresh (35% of window width)
                DisplayMemberBinding = new System.Windows.Data.Binding("Name")
            };
            gridView.Columns.Add(fileColumn);

            // Size column (with converter) - always use fresh width calculation (no saved widths)
            if (_appConfig.ShowSizeColumn)
            {
                var sizeColumn2 = new GridViewColumn
                {
                    Header = CreateSortableHeader("Size"),
                    Width = 80, // Wider to prevent cutoff
                    DisplayMemberBinding = new System.Windows.Data.Binding("Size") { Converter = new AstroImages.Wpf.Converters.FileSizeConverter() }
                };
                gridView.Columns.Add(sizeColumn2);
            }

            // Custom keyword columns (green) - each has individual width based on keyword length
            foreach (var keyword in _appConfig.CustomKeywords)
            {
                var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                
                // Use SafeDictionaryConverter with parameter to avoid KeyNotFoundException
                var binding = new System.Windows.Data.Binding("CustomKeywords");
                binding.Converter = new SafeDictionaryConverter();
                binding.ConverterParameter = keyword;
                
                factory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, binding);
                // Bind to theme resource so foreground color updates when theme changes
                var foregroundBinding = new System.Windows.Data.Binding();
                foregroundBinding.Source = System.Windows.Application.Current;
                foregroundBinding.Path = new System.Windows.PropertyPath("Resources[ThemeAccentGreen]");
                factory.SetBinding(System.Windows.Controls.TextBlock.ForegroundProperty, foregroundBinding);
                factory.SetValue(System.Windows.Controls.TextBlock.TextTrimmingProperty, System.Windows.TextTrimming.CharacterEllipsis);
                var template = new DataTemplate { VisualTree = factory };
                
                // Calculate width based on actual data content - different for each keyword
                double calculatedWidth = CalculateDataDrivenColumnWidth(keyword, true);
                
                var column = new GridViewColumn
                {
                    Header = CreateSortableHeader(keyword),
                    Width = calculatedWidth,
                    CellTemplate = template
                };
                gridView.Columns.Add(column);
            }

            // FITS keyword columns (blue) - each has individual width based on keyword length
            foreach (var keyword in _appConfig.FitsKeywords)
            {
                var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
                
                // Use SafeDictionaryConverter with parameter to avoid KeyNotFoundException
                var binding = new System.Windows.Data.Binding("FitsKeywords");
                binding.Converter = new SafeDictionaryConverter();
                binding.ConverterParameter = keyword;
                
                factory.SetBinding(System.Windows.Controls.TextBlock.TextProperty, binding);
                // Bind to theme resource so foreground color updates when theme changes
                var foregroundBinding = new System.Windows.Data.Binding();
                foregroundBinding.Source = System.Windows.Application.Current;
                foregroundBinding.Path = new System.Windows.PropertyPath("Resources[ThemeAccentBlue]");
                factory.SetBinding(System.Windows.Controls.TextBlock.ForegroundProperty, foregroundBinding);
                factory.SetValue(System.Windows.Controls.TextBlock.TextTrimmingProperty, System.Windows.TextTrimming.CharacterEllipsis);
                var template = new DataTemplate { VisualTree = factory };
                
                // Calculate width based on actual data content - different for each keyword
                double calculatedWidth = CalculateDataDrivenColumnWidth(keyword, false);
                
                var column = new GridViewColumn
                {
                    Header = CreateSortableHeader(keyword),
                    Width = calculatedWidth,
                    CellTemplate = template
                };
                gridView.Columns.Add(column);
            }

            // Skip automatic auto-resize to keep columns narrow
            // Auto-resize will only happen when explicitly called via AutoResizeColumns() method
            
            // Force all columns to use their calculated optimal widths (ignore any cached widths)
            ResetAllColumnWidths();
        }

        /// <summary>
        /// Updates the File column width to be responsive to the current window size (25% of window width)
        /// </summary>
        public void UpdateFileColumnWidth()
        {
            if (_fileListView?.View is GridView gridView)
            {
                var fileColumn = gridView.Columns.FirstOrDefault(c => 
                    c.Header is GridViewColumnHeader header && 
                    header.Content?.ToString() == "File");
                    
                if (fileColumn != null)
                {
                    fileColumn.Width = GetFileColumnWidth();
                }
            }
        }

        /// <summary>
        /// Forces all columns to reset to their calculated optimal widths.
        /// This ensures no cached/saved widths are used - everything is recalculated fresh.
        /// Each column gets individually calculated width to avoid uniform sizing.
        /// </summary>
        public void ResetAllColumnWidths()
        {
            if (_fileListView?.View is GridView gridView)
            {
                foreach (var column in gridView.Columns)
                {
                    if (column.Header is GridViewColumnHeader header)
                    {
                        var headerText = header.Content?.ToString() ?? "";
                        double newWidth;
                        
                        switch (headerText.ToLowerInvariant())
                        {
                            case "file":
                                newWidth = GetFileColumnWidth();
                                break;
                            case "size":
                                newWidth = 85; // Fixed width for size column
                                break;
                            case "":
                                // Button/checkbox columns - keep existing width (usually 30-35px)
                                newWidth = column.Width < 30 ? 30 : (column.Width > 40 ? 35 : column.Width);
                                break;
                            default:
                                // Keyword columns - use data-driven calculation based on actual content
                                // Check if it's a custom keyword first
                                bool isCustom = _appConfig.CustomKeywords.Contains(headerText);
                                newWidth = CalculateDataDrivenColumnWidth(headerText, isCustom);
                                break;
                        }
                        
                        column.Width = newWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates column width based on actual data content in the current file list.
        /// Analyzes all values in the column to determine optimal width.
        /// </summary>
        /// <param name="columnName">The column name/header</param>
        /// <param name="isCustomKeyword">True if this is a custom keyword column</param>
        /// <returns>Optimal width based on actual content</returns>
        private double CalculateDataDrivenColumnWidth(string columnName, bool isCustomKeyword = false)
        {
            if (string.IsNullOrEmpty(columnName))
                return 60;

            // Get the file items from the ViewModel
            var fileItems = GetFileItemsFromDataContext();
            if (fileItems == null || !fileItems.Any())
            {
                // No data - fall back to header-based calculation
                return Math.Max(60, columnName.Length * 7 + 20);
            }

            // Calculate header width (minimum width needed)
            double headerWidth = MeasureTextWidth(columnName) + 20; // Add padding for sorting arrows

            // Calculate maximum content width
            double maxContentWidth = 0;
            
            foreach (var item in fileItems)
            {
                string? content = null;
                
                // Get content based on column type
                switch (columnName.ToLowerInvariant())
                {
                    case "file":
                        content = item.Name;
                        break;
                    case "size":
                        content = FormatFileSize(item.Size);
                        break;
                    default:
                        // Keyword column - check both custom and FITS keywords
                        if (isCustomKeyword && item.CustomKeywords.ContainsKey(columnName))
                        {
                            content = item.CustomKeywords[columnName];
                        }
                        else if (!isCustomKeyword && item.FitsKeywords.ContainsKey(columnName))
                        {
                            content = item.FitsKeywords[columnName];
                        }
                        break;
                }
                
                if (!string.IsNullOrEmpty(content))
                {
                    // Add more generous padding for keyword columns to prevent cutoff
                    double padding = columnName.Length > 6 ? 20 : 15; // More padding for longer column names
                    double contentWidth = MeasureTextWidth(content) + padding;
                    maxContentWidth = Math.Max(maxContentWidth, contentWidth);
                }
            }
            
            // Use the larger of header width or content width, with appropriate limits
            double optimalWidth = Math.Max(headerWidth, maxContentWidth);
            
            // Special handling for File column - should be 25% of window width
            if (columnName.ToLowerInvariant() == "file")
            {
                return GetFileColumnWidth(); // Use responsive width calculation
            }
            
            // Apply reasonable min/max limits for other columns
            optimalWidth = Math.Max(optimalWidth, 40);  // Minimum 40px
            // Increase maximum to prevent cutoff for longer values (FILTER, IMAGETYP, etc.)
            optimalWidth = Math.Min(optimalWidth, 300); // Maximum 300px (was 200px)
            
            return Math.Round(optimalWidth);
        }

        /// <summary>
        /// Gets the file items from the ListView's DataContext (MainWindowViewModel).
        /// </summary>
        /// <returns>Collection of FileItem objects or null if not available</returns>
        private IEnumerable<Models.FileItem>? GetFileItemsFromDataContext()
        {
            if (_fileListView?.DataContext is MainWindowViewModel viewModel)
            {
                // Access the Files property directly (it's an ObservableCollection<FileItem>)
                return viewModel.Files;
            }
            return null;
        }

        /// <summary>
        /// Measures the width of text using WPF's text measurement.
        /// </summary>
        /// <param name="text">Text to measure</param>
        /// <returns>Width in pixels</returns>
        private double MeasureTextWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var formattedText = new System.Windows.Media.FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Segoe UI"), // Default font
                13, // Font size - match ListView font size
                System.Windows.Media.Brushes.Black,
                new System.Windows.Media.NumberSubstitution(),
                1); // PixelsPerDip

            return formattedText.Width;
        }

        /// <summary>
        /// Formats file size for display (used in width calculation).
        /// </summary>
        /// <param name="sizeInBytes">File size in bytes</param>
        /// <returns>Formatted size string</returns>
        private string FormatFileSize(long sizeInBytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (sizeInBytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(sizeInBytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }

        /// <summary>
        /// Calculates optimal column widths based on content and headers.
        /// Returns a dictionary with column identifiers as keys and optimal widths as values.
        /// NOTE: These widths are calculated fresh each time - no saved widths are used.
        /// </summary>
        private Dictionary<string, double> CalculateOptimalColumnWidths()
        {
            var widths = new Dictionary<string, double>();
            
            // Always use fresh calculations - never load saved column widths
            // This ensures columns are always sized based on current data and window size
            widths["File"] = GetFileColumnWidth();  // Responsive filename column (35% of window width)
            widths["Size"] = 85;       // Fixed width for size column
            
            // Custom and FITS keyword columns - data-driven widths based on actual content
            foreach (var keyword in _appConfig.CustomKeywords)
            {
                widths[$"Custom_{keyword}"] = CalculateDataDrivenColumnWidth(keyword, true);
            }
            
            foreach (var keyword in _appConfig.FitsKeywords)
            {
                widths[$"FITS_{keyword}"] = CalculateDataDrivenColumnWidth(keyword, false);
            }
            
            return widths;
        }

        /// <summary>
        /// Calculates the total width needed for all columns in the file list.
        /// Includes padding for ListView margins, borders, and internal spacing.
        /// </summary>
        /// <returns>Total width in pixels needed for all columns plus necessary padding</returns>
        public double CalculateTotalColumnsWidth()
        {
            double totalWidth = 0;
            int columnCount = 0;
            
            if (_fileListView?.View is GridView gridView)
            {
                foreach (var column in gridView.Columns)
                {
                    totalWidth += column.Width;
                    columnCount++;
                }
                
                // Account for ListView margins, borders, and internal spacing
                // ListView margins (4px left + 4px right) + borders + ScrollViewer padding + header spacing
                double padding = 43; // 8px margins + ~25px for borders, scrollviewer, and internal spacing
                totalWidth += padding;
            }
            
            return totalWidth;
        }

        /// <summary>
        /// Recalculates all column widths based on the actual data content in the current file list.
        /// This should be called whenever the file list changes to ensure optimal column sizing.
        /// </summary>
        public void RecalculateColumnWidthsFromData()
        {
            if (_fileListView?.View is GridView gridView)
            {
                var fileItems = GetFileItemsFromDataContext();
                if (fileItems != null && fileItems.Any())
                {
                    foreach (var column in gridView.Columns)
                    {
                        if (column.Header is GridViewColumnHeader header)
                        {
                            var headerText = header.Content?.ToString() ?? "";
                            double newWidth;
                            
                            switch (headerText.ToLowerInvariant())
                            {
                                case "file":
                                    newWidth = CalculateDataDrivenColumnWidth(headerText, false);
                                    break;
                                case "size":
                                    newWidth = CalculateDataDrivenColumnWidth(headerText, false);
                                    break;
                                case "":
                                    // Button/checkbox columns - keep existing width
                                    continue;
                                default:
                                    // Keyword columns - determine if custom or FITS keyword
                                    bool isCustom = _appConfig.CustomKeywords.Contains(headerText);
                                    newWidth = CalculateDataDrivenColumnWidth(headerText, isCustom);
                                    break;
                            }
                            
                            column.Width = newWidth;

                        }
                    }
                    
                    // After calculating column widths, adjust the splitter position
                    AdjustSplitterPosition();
                }
            }
        }

        /// <summary>
        /// Public method to adjust splitter position for optimal file list width.
        /// </summary>
        public void AdjustSplitterForOptimalWidth()
        {
            AdjustSplitterPosition();
        }

        /// <summary>
        /// Adjusts the GridSplitter position so the file list is just wide enough to show all columns
        /// without needing a horizontal scrollbar, but never more than 70% of the window width.
        /// </summary>
        private void AdjustSplitterPosition()
        {
            // Try to find the main window and its GridSplitter
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                // Find the main Grid with the 3 columns (file list, splitter, image viewer)
                // Look for the Grid in row 1 (the main content area, not the menu)
                var mainGrid = FindMainContentGrid(mainWindow);
                if (mainGrid != null && mainGrid.ColumnDefinitions.Count >= 3)
                {


                    double totalColumnsWidth = CalculateTotalColumnsWidth();

                    double windowWidth = mainWindow.ActualWidth;
                    
                    // Calculate what proportion of the window the file list should take
                    // Small buffer for rounding/layout issues (reduced since padding is now more accurate)
                    double bufferedColumnsWidth = totalColumnsWidth + 3;
                    double idealRatio = bufferedColumnsWidth / windowWidth;
                    
                    // Apply constraints: minimum 30%, maximum 70%
                    double fileListRatio = Math.Max(0.3, Math.Min(0.7, idealRatio));
                    double imageViewerRatio = 1.0 - fileListRatio;
                    
                    // Set the column definitions
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(fileListRatio, GridUnitType.Star);
                    mainGrid.ColumnDefinitions[2].Width = new GridLength(imageViewerRatio, GridUnitType.Star);
                    
                    // Force layout update to get actual sizes
                    mainWindow.UpdateLayout();
                    
                    // Get actual file list pane width after layout
                    double actualFileListWidth = 0;
                    if (_fileListView?.ActualWidth > 0)
                    {
                        actualFileListWidth = _fileListView.ActualWidth;
                    }
                    
                    // Calculate the difference between what we need and what we got
                    double widthDifference = totalColumnsWidth - actualFileListWidth;
                    
                    // Log detailed debugging information
                    bool willHaveScrollbar = idealRatio > 0.7;

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Could not find main content grid for splitter adjustment");
                }
            }
        }

        /// <summary>
        /// Finds the main content grid (the one with 3 columns: file list, splitter, image viewer).
        /// This is more specific than the generic FindChild method.
        /// </summary>
        private Grid? FindMainContentGrid(DependencyObject parent)
        {
            // Look for a Grid with exactly 3 column definitions
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Grid grid && grid.ColumnDefinitions.Count == 3)
                {
                    // Check if this looks like our main content grid by seeing if it has a GridSplitter
                    bool hasGridSplitter = false;
                    for (int j = 0; j < VisualTreeHelper.GetChildrenCount(grid); j++)
                    {
                        var gridChild = VisualTreeHelper.GetChild(grid, j);
                        if (gridChild is GridSplitter)
                        {
                            hasGridSplitter = true;
                            break;
                        }
                    }
                    
                    if (hasGridSplitter)
                    {

                        
                        // Hook up splitter event if we haven't already
                        SetupSplitterMonitoring(grid);
                        
                        return grid;
                    }
                }

                var result = FindMainContentGrid(child);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Sets up monitoring for manual splitter movements to debug actual pane sizes.
        /// </summary>
        private void SetupSplitterMonitoring(Grid mainGrid)
        {
            // Find the GridSplitter if we haven't already hooked it up
            if (_gridSplitter == null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(mainGrid); i++)
                {
                    var child = VisualTreeHelper.GetChild(mainGrid, i);
                    if (child is GridSplitter splitter)
                    {
                        _gridSplitter = splitter;
                        
                        // Hook up to DragCompleted event to detect manual moves
                        _gridSplitter.DragCompleted += GridSplitter_DragCompleted;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the user manually moves the splitter.
        /// </summary>
        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Force layout update after manual splitter adjustment
            System.Windows.Application.Current.MainWindow?.UpdateLayout();
        }

        /// <summary>
        /// Helper method to find a child control of a specific type in the visual tree.
        /// </summary>
        private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;

                var childResult = FindChild<T>(child);
                if (childResult != null) return childResult;
            }
            return null;
        }

        /// <summary>
        /// Applies our compact column widths (replaces auto-resize to prevent wide columns).
        /// Instead of auto-sizing to content, this uses our optimized compact widths.
        /// </summary>
        public void AutoResizeColumns()
        {
            // Use data-driven calculation for optimal widths
            RecalculateColumnWidthsFromData();
            
            // Schedule multiple attempts to ensure splitter adjustment happens after layout
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                AdjustSplitterPosition();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            
            // Also try with lower priority as backup
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                AdjustSplitterPosition();
            }), System.Windows.Threading.DispatcherPriority.Background);
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
                        
                        // Apply minimum width constraints and minimal padding
                        var minWidth = GetMinimumColumnWidth(column);
                        var maxWidth = GetMaximumColumnWidth(column);  // Prevent columns from getting too wide
                        var finalWidth = Math.Min(maxWidth, Math.Max(minWidth, autoWidth + 2)); // Only 2px padding and cap maximum
                        
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
        /// Gets the responsive width for the File column (35% of window width)
        /// </summary>
        private double GetFileColumnWidth()
        {
            double targetWidth = 280; // Default fallback (increased from 200)

            // Try to get the main window width first
            if (System.Windows.Application.Current.MainWindow?.ActualWidth > 0)
            {
                targetWidth = System.Windows.Application.Current.MainWindow.ActualWidth * 0.35;
            }
            // Then try ListView width as secondary option
            else if (_fileListView?.ActualWidth > 0)
            {
                targetWidth = _fileListView.ActualWidth * 0.35;
            }
            

            
            // Ensure reasonable bounds: minimum 140px, maximum 400px (increased from 120px/300px)
            return Math.Max(140, Math.Min(500, targetWidth));
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
                    return GetFileColumnWidth(); // Responsive to window width (35%)
                case "size":
                    return 50;  // Just enough for "Size" header + values like "1.2 MB"
                default:
                    return Math.Max(40, header.Length * 4 + 5); // Very tight for keyword columns
            }
        }
        
        /// <summary>
        /// Gets the maximum width for a specific column to prevent them from getting too wide
        /// </summary>
        private double GetMaximumColumnWidth(GridViewColumn column)
        {
            // Checkbox and button columns should stay narrow
            if (string.IsNullOrEmpty(column.Header?.ToString()))
            {
                return column.Width < 32 ? 30 : 35;
            }
            
            var header = column.Header?.ToString() ?? "";
            
            // Set maximum widths to keep columns very compact
            switch (header.ToLowerInvariant())
            {
                case "file":
                    return GetFileColumnWidth() + 20; // File column can be a bit wider than minimum
                case "size":
                    return 60;  // Size should stay narrow
                default:
                    return Math.Min(80, Math.Max(45, header.Length * 6)); // Keyword columns very compact
            }
        }
    }
}
