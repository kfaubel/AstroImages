using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ApexAstro.Wpf
{
    public partial class GraphSettingsDialog : Window
    {
        public string SelectedXColumn { get; private set; } = string.Empty;
        public List<string> SelectedYColumns { get; private set; } = new();
        public string SelectedChartType { get; private set; } = "Scatter";

        public GraphSettingsDialog(
            IEnumerable<string> availableColumns,
            string currentX,
            IEnumerable<string> currentY,
            string currentChartType)
        {
            InitializeComponent();

            // Populate Y list
            var cols = availableColumns
                .Where(c => !string.Equals(c, "Date", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(c, "Time", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(c, "Filter", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(c, "Frame", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var yCurrent = new HashSet<string>(currentY);
            foreach (var c in cols)
            {
                var item = new ListBoxItem { Content = c };
                if (yCurrent.Contains(c))
                    item.IsSelected = true;
                YAxisList.Items.Add(item);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var ySelected = YAxisList.SelectedItems
                .Cast<ListBoxItem>()
                .Select(i => i.Content?.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (ySelected.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one Y axis column.", "Graph Settings",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedXColumn   = "Time";
            SelectedYColumns  = ySelected;
            SelectedChartType = "Line";
            DialogResult      = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
