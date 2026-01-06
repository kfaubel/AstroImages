using System.Windows;

namespace AstroImages.Wpf
{
    /// <summary>
    /// GetStartedDialog - shows a welcome/getting started dialog when the application starts with no files loaded.
    /// </summary>
    public partial class GetStartedDialog : Window
    {
        // Delegate to open folder
        public delegate void OpenFolderDelegate();
        
        // Event raised when user clicks "Open Folder"
        public event OpenFolderDelegate? OpenFolderRequested;

        /// <summary>
        /// Constructor
        /// </summary>
        public GetStartedDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the "Open Folder" button click
        /// </summary>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderRequested?.Invoke();
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles the "Cancel" button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
