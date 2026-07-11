using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ApexAstro.Wpf.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfApp = System.Windows.Application;

namespace ApexAstro.Wpf
{
    public partial class GraphWindow : Window
    {
        private readonly AppConfig _appConfig;
        private readonly Func<IReadOnlyList<FileItem>> _getItems;
        private readonly Func<IEnumerable<string>> _getAvailableColumns;

        // Maps each OxyPlot series to its ordered list of source FileItems
        private readonly Dictionary<OxyPlot.Series.Series, List<FileItem>> _seriesToItems = new();
        private PlotModel? _currentModel;

        public GraphWindow(
            AppConfig appConfig,
            Func<IReadOnlyList<FileItem>> getItems,
            Func<IEnumerable<string>> getAvailableColumns)
        {
            InitializeComponent();
            _appConfig           = appConfig;
            _getItems            = getItems;
            _getAvailableColumns = getAvailableColumns;

            ApplySavedWindowSize();

            // Disable OxyPlot's built-in tracker popup (yellow tooltip).
            // We use the custom themed hover popup rendered in this window instead.
            var controller = new PlotController();
            controller.UnbindAll();
            PlotView.Controller = controller;

            // Wire mouse events once — work across all model rebuilds
            PlotView.MouseMove  += OnPlotMouseMove;
            PlotView.PreviewMouseDown += OnPlotMouseDown;
            PlotView.MouseLeave += (_, _) => HoverBorder.Visibility = Visibility.Collapsed;
            HoverCanvas.MouseLeave += (_, _) => HoverBorder.Visibility = Visibility.Collapsed;
            SizeChanged         += GraphWindow_SizeChanged;
            LocationChanged     += GraphWindow_LocationChanged;
            Closed              += GraphWindow_Closed;

            BuildChart();
        }

        // -- Settings dialog --------------------------------------------------
        public bool OpenSettings()
        {
            var dlg = new GraphSettingsDialog(
                _getAvailableColumns(),
                _appConfig.GraphXColumn,
                _appConfig.GraphYColumns,
                _appConfig.GraphChartType)
            { Owner = this };

            if (dlg.ShowDialog() != true) return false;

            _appConfig.GraphXColumn   = dlg.SelectedXColumn;
            _appConfig.GraphYColumns  = dlg.SelectedYColumns;
            _appConfig.GraphChartType = "Line";
            _appConfig.Save();

            BuildChart();
            return true;
        }

        // -- Build chart ------------------------------------------------------
        private void BuildChart()
        {
            _seriesToItems.Clear();
            HoverBorder.Visibility = Visibility.Collapsed;

            var items  = _getItems();
            var yCols  = _appConfig.GraphYColumns;

            bool isDark    = IsDarkTheme();
            OxyColor fore  = isDark ? OxyColors.WhiteSmoke : OxyColors.Black;
            OxyColor bg    = isDark ? OxyColor.FromRgb(30, 30, 30) : OxyColors.White;
            OxyColor faint = OxyColor.FromArgb(60, 200, 200, 200);

            var model = new PlotModel
            {
                Background = bg,
                TextColor  = fore,
                PlotAreaBorderColor = fore,
                TitleColor = fore,
            };

            var xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom, Title = string.Empty,
                StringFormat = "MM-dd HH:mm",
                TextColor = fore, TitleColor = fore, TicklineColor = fore,
                MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = faint,
            };

            model.Axes.Add(xAxis);

            var palette = OxyPalettes.HueDistinct(Math.Max(1, yCols.Count)).Colors;
            var yAxisKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < yCols.Count; i++)
            {
                var yCol = yCols[i];
                var color = MakeSeriesBaseColor(palette[i % palette.Count]);
                var axis = new LinearAxis
                {
                    Key = $"Y{i}",
                    Position = AxisPosition.Left,
                    PositionTier = i,
                    Title = yCol,
                    TextColor = color,
                    TitleColor = color,
                    TicklineColor = color,
                    AxislineColor = color,
                    AxislineStyle = LineStyle.Solid,
                    Minimum = 0,
                    AbsoluteMinimum = 0,
                    MajorGridlineStyle = i == 0 ? LineStyle.Dot : LineStyle.None,
                    MajorGridlineColor = faint,
                };

                model.Axes.Add(axis);
                yAxisKeys[yCol] = axis.Key;
            }

            // Y series
            for (int i = 0; i < yCols.Count; i++)
            {
                var yCol = yCols[i];
                var color = MakeSeriesBaseColor(palette[i % palette.Count]);

                // Build ordered (time, y, item) tuples; sort for line charts
                var pts = items
                    .Where(it => TryGetGraphDateTimeValue(it, out _) && TryGetNumeric(it, yCol, out _))
                    .Select(it =>
                    {
                        TryGetGraphDateTimeValue(it, out DateTime timestamp);
                        TryGetNumeric(it, yCol, out double yv);
                        return (xv: DateTimeAxis.ToDouble(timestamp), yv, it);
                    })
                    .ToList();
                pts.Sort((a, b) => a.xv.CompareTo(b.xv));

                var s = new LineSeries
                {
                    Title = yCol,
                    MarkerType = MarkerType.None,
                    StrokeThickness = 1.2,
                    Color = color,
                    YAxisKey = yAxisKeys[yCol],
                };
                foreach (var (xv, yv, _) in pts)
                {
                    s.Points.Add(new DataPoint(xv, yv));
                }
                model.Series.Add(s);

                var unmarked = new ScatterSeries
                {
                    Title = string.Empty,
                    RenderInLegend = false,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 2.2,
                    MarkerFill = color,
                    MarkerStroke = color,
                    YAxisKey = yAxisKeys[yCol],
                };
                var marked = new ScatterSeries
                {
                    Title = string.Empty,
                    RenderInLegend = false,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4.8,
                    MarkerFill = OxyColors.Red,
                    MarkerStroke = OxyColors.Red,
                    YAxisKey = yAxisKeys[yCol],
                };

                var unmarkedItems = new List<FileItem>();
                var markedItems = new List<FileItem>();
                foreach (var (xv, yv, item) in pts)
                {
                    if (item.IsSelected)
                    {
                        marked.Points.Add(new ScatterPoint(xv, yv));
                        markedItems.Add(item);
                    }
                    else
                    {
                        unmarked.Points.Add(new ScatterPoint(xv, yv));
                        unmarkedItems.Add(item);
                    }
                }

                _seriesToItems[unmarked] = unmarkedItems;
                _seriesToItems[marked] = markedItems;
                model.Series.Add(unmarked);
                model.Series.Add(marked);
            }

            _currentModel = model;

            PlotView.Model = model;
        }

        // -- Tracker (hover tooltip) ------------------------------------------
        private void OnPlotMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentModel == null || _seriesToItems.Count == 0) return;

            var pos = e.GetPosition(PlotView);
            var sp  = new ScreenPoint(pos.X, pos.Y);

            foreach (var series in _currentModel.Series)
            {
                var hit = series.HitTest(new HitTestArguments(sp, 15));
                if (hit == null) continue;
                int idx = (int)Math.Round(hit.Index);
                if (_seriesToItems.TryGetValue(series, out var items) && idx >= 0 && idx < items.Count)
                {
                    ShowHoverContent(items[idx], sp);
                    return;
                }
            }
            HoverBorder.Visibility = Visibility.Collapsed;
        }

        private void ShowHoverContent(FileItem item, ScreenPoint pos)
        {
            HoverContent.Children.Clear();

            HoverContent.Children.Add(new TextBlock
            {
                Text = item.IsSelected ? "Marked (Click to toggle)" : "Unmarked (Click to toggle)",
                FontWeight = System.Windows.FontWeights.SemiBold,
                Foreground = item.IsSelected
                    ? GetBrush("ThemeAccentGreen", GetBrush("ThemeForeground", WpfBrushes.Black))
                    : GetBrush("ThemeSecondaryText", WpfBrushes.Gray),
                Margin = new Thickness(0, 0, 0, 6),
            });

            // ---- filename header ----
            HoverContent.Children.Add(new TextBlock
            {
                Text       = item.Name,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = GetBrush("ThemeForeground", WpfBrushes.Black),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth   = 240,
                Margin     = new Thickness(0, 0, 0, 4),
            });

            // ---- all non-empty columns ----
            AddRowIfNotEmpty("Date",   item.FilenameDate);
            AddRowIfNotEmpty("Time",   item.FilenameTime);
            AddRowIfNotEmpty("Filter", ExtractFilterLetter(item));
            AddRowIfNotEmpty("Frame",  item.FilenameFrame);
            if (item.Size > 0)
                AddRow("Size", FormatSize(item.Size));
            if (item.Median.HasValue)
                AddRow("Median", item.Median.Value.ToString("G6", CultureInfo.InvariantCulture));
            if (item.Mean.HasValue)
                AddRow("Mean", item.Mean.Value.ToString("G6", CultureInfo.InvariantCulture));

            foreach (var kv in item.CustomKeywords.Where(kv => !string.IsNullOrEmpty(kv.Value)))
                AddRow(kv.Key, kv.Value);
            foreach (var kv in item.FitsKeywords.Where(kv => !string.IsNullOrEmpty(kv.Value)))
                AddRow(kv.Key, kv.Value);
            foreach (var kv in item.CsvKeywords.Where(kv => !string.IsNullOrEmpty(kv.Value)))
                AddRow(kv.Key, kv.Value);

            // ---- position the overlay ----
            double left = pos.X + 14;
            double top  = pos.Y + 14;

            // Ensure it stays within the canvas bounds
            HoverBorder.Measure(new System.Windows.Size(280, 400));
            double w = HoverBorder.DesiredSize.Width;
            double h = HoverBorder.DesiredSize.Height;
            if (HoverCanvas.ActualWidth > 0 && left + w > HoverCanvas.ActualWidth - 8)
                left = pos.X - w - 8;
            if (HoverCanvas.ActualHeight > 0 && top + h > HoverCanvas.ActualHeight - 8)
                top  = pos.Y - h - 8;

            Canvas.SetLeft(HoverBorder, Math.Max(0, left));
            Canvas.SetTop(HoverBorder,  Math.Max(0, top));
            HoverBorder.Visibility = Visibility.Visible;
        }

        private void OnPlotMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_currentModel == null || _seriesToItems.Count == 0)
            {
                return;
            }

            var pos = e.GetPosition(PlotView);
            var sp = new ScreenPoint(pos.X, pos.Y);

            foreach (var series in _currentModel.Series)
            {
                var hit = series.HitTest(new HitTestArguments(sp, 8));
                if (hit == null)
                {
                    continue;
                }

                int idx = (int)Math.Round(hit.Index);
                if (_seriesToItems.TryGetValue(series, out var items) && idx >= 0 && idx < items.Count)
                {
                    var item = items[idx];
                    item.IsSelected = !item.IsSelected;
                    ShowHoverContent(item, sp);
                    BuildChart();
                    e.Handled = true;
                    return;
                }
            }
        }

        private void AddRowIfNotEmpty(string label, string value)
        {
            if (!string.IsNullOrEmpty(value)) AddRow(label, value);
        }

        private void AddRow(string label, string value)
        {
            var row = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 52 });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text       = label + ":",
                Foreground = GetBrush("ThemeSecondaryText", WpfBrushes.Gray),
                Margin     = new Thickness(0, 0, 8, 0),
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
            };
            var val = new TextBlock
            {
                Text         = value,
                Foreground   = GetBrush("ThemeForeground", WpfBrushes.Black),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth     = 180,
            };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            row.Children.Add(lbl);
            row.Children.Add(val);
            HoverContent.Children.Add(row);
        }

        private WpfBrush GetBrush(string key, WpfBrush fallback)
            => TryFindResource(key) as WpfBrush ?? fallback;

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024)     return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }

        // -- Helpers ----------------------------------------------------------
        private static string ExtractString(FileItem item, string col) => col switch
        {
            "Date"   => item.FilenameDate,
            "Time"   => item.FilenameTime,
            "Filter" => ExtractFilterLetter(item),
            "Frame"  => item.FilenameFrame,
            "Size"   => item.Size.ToString(),
            "Median" => item.Median?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "Mean"   => item.Mean?.ToString(CultureInfo.InvariantCulture)   ?? string.Empty,
            _        => item.CustomKeywords.TryGetValue(col, out var cv) ? cv
                      : item.FitsKeywords .TryGetValue(col, out var fv) ? fv
                      : item.CsvKeywords  .TryGetValue(col, out var sv) ? sv
                      : string.Empty
        };

        private static bool TryGetNumeric(FileItem item, string col, out double value)
        {
            var raw = ExtractString(item, col);
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetGraphDateTimeValue(FileItem item, out DateTime value)
        {
            value = DateTime.MinValue;

            if (DateTime.TryParseExact(
                    item.FilenameDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date)
                && TimeSpan.TryParse(item.FilenameTime, out var time))
            {
                value = date.Date.Add(time);
                return true;
            }

            if (DateTime.TryParse(item.FilenameDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                && TimeSpan.TryParse(item.FilenameTime, out time))
            {
                value = date.Date.Add(time);
                return true;
            }

            if (TimeSpan.TryParse(item.FilenameTime, out time))
            {
                value = DateTime.Today.Add(time);
                return true;
            }

            return false;
        }

        private static OxyColor ToMutedColor(OxyColor source)
        {
            byte r = (byte)((source.R * 0.55) + (128 * 0.45));
            byte g = (byte)((source.G * 0.55) + (128 * 0.45));
            byte b = (byte)((source.B * 0.55) + (128 * 0.45));
            return OxyColor.FromAColor(190, OxyColor.FromRgb(r, g, b));
        }

        private static OxyColor MakeSeriesBaseColor(OxyColor source)
        {
            var muted = ToMutedColor(source);

            // Keep red reserved for mark state by shifting naturally red hues toward magenta.
            if (muted.R > 170 && muted.G < 120 && muted.B < 120)
            {
                return OxyColor.FromAColor(muted.A, OxyColor.FromRgb(150, 110, 175));
            }

            return muted;
        }

        private static bool TryGetGraphTimeValue(FileItem item, out DateTime value)
        {
            value = DateTime.MinValue;
            if (!TimeSpan.TryParse(item.FilenameTime, out var time))
            {
                return false;
            }

            value = DateTime.Today.Add(time);
            return true;
        }

        private static string ExtractFilterLetter(FileItem item)
        {
            if (TryGetKeywordValue(item.FitsKeywords, "FILTER", out var rawFits))
            {
                var normalized = NormalizeFilterValue(rawFits);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }
            }

            var fallback = NormalizeFilterValue(ParseFilterFromFilename(item.Name));
            return fallback;
        }

        private static bool TryGetKeywordValue(Dictionary<string, string> keywords, string key, out string value)
        {
            foreach (var kv in keywords)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }

        private static string ParseFilterFromFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return string.Empty;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return string.Empty;
            }

            var tokens = nameWithoutExtension.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 2)
            {
                return tokens[2];
            }

            return string.Empty;
        }

        private static string NormalizeFilterValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var compact = raw.Trim().ToUpperInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
            return compact switch
            {
                "L" or "LUM" or "LUMINANCE" => "L",
                "R" or "RED" => "R",
                "G" or "GREEN" => "G",
                "B" or "BLUE" => "B",
                "S" or "SII" or "S2" => "S",
                "H" or "HA" or "HALPHA" or "HALPHA7" => "H",
                "O" or "OIII" or "O3" => "O",
                _ => compact.Length == 1 && "LRGBSHO".Contains(compact, StringComparison.Ordinal) ? compact : string.Empty
            };
        }

        private static bool IsDarkTheme()
        {
            if (WpfApp.Current.TryFindResource("ThemeWindowBackground") is SolidColorBrush bg)
                return (bg.Color.R + bg.Color.G + bg.Color.B) < 384;
            return false;
        }

        private void ApplySavedWindowSize()
        {
            if (_appConfig.GraphWindowWidth >= MinWidth)
            {
                Width = _appConfig.GraphWindowWidth;
            }

            if (_appConfig.GraphWindowHeight >= MinHeight)
            {
                Height = _appConfig.GraphWindowHeight;
            }

            if (!double.IsNaN(_appConfig.GraphWindowLeft))
            {
                Left = _appConfig.GraphWindowLeft;
            }

            if (!double.IsNaN(_appConfig.GraphWindowTop))
            {
                Top = _appConfig.GraphWindowTop;
            }
        }

        private void GraphWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                return;
            }

            if (ActualWidth >= MinWidth)
            {
                _appConfig.GraphWindowWidth = ActualWidth;
            }

            if (ActualHeight >= MinHeight)
            {
                _appConfig.GraphWindowHeight = ActualHeight;
            }
        }

        private void GraphWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                return;
            }

            _appConfig.GraphWindowLeft = Left;
            _appConfig.GraphWindowTop = Top;
        }

        private void GraphWindow_Closed(object? sender, EventArgs e)
        {
            _appConfig.Save();
        }

        // -- Button handlers --------------------------------------------------
        private void ModifyButton_Click(object sender, RoutedEventArgs e) => OpenSettings();
    }
}