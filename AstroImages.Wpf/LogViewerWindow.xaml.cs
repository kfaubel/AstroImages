using System.Windows;
using AstroImages.Wpf.Services;

namespace AstroImages.Wpf
{
    public partial class LogViewerWindow : Window
    {
        private readonly ILoggingService _loggingService;

        public LogViewerWindow(ILoggingService loggingService)
        {
            InitializeComponent();
            _loggingService = loggingService;
            LoadLog();
        }

        private void LoadLog()
        {
            LogTextBox.Text = _loggingService.GetLogContents();
            // Scroll to bottom to show latest entries
            LogTextBox.ScrollToEnd();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLog();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to clear the activity log?",
                "Clear Log",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _loggingService.ClearLog();
                LoadLog();
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LogTextBox.Text))
            {
                System.Windows.Clipboard.SetText(LogTextBox.Text);
                System.Windows.MessageBox.Show("Log copied to clipboard.", "Copy to Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
