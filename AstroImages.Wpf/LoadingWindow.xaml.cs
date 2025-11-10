using System.Windows;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// Simple modal dialog to show loading progress
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow(string message = "Loading images...")
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        /// <summary>
        /// Updates the progress bar and message
        /// </summary>
        public void UpdateProgress(int current, int total)
        {
            if (total > 0)
            {
                LoadProgressBar.Maximum = total;
                LoadProgressBar.Value = current;
                LoadProgressBar.IsIndeterminate = false;
                MessageText.Text = $"Loading images... {current} of {total}";
            }
        }
    }
}
