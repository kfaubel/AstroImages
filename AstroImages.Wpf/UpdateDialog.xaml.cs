using System.Windows;
using AstroImages.Wpf.Services;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Dialog window for notifying users about available updates
    /// </summary>
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly UpdateService _updateService;

        public bool DisableUpdateChecks { get; private set; }

        public UpdateDialog(UpdateInfo updateInfo, UpdateService updateService)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = updateService;

            // Populate the dialog with update information
            VersionText.Text = $"Version {_updateInfo.LatestVersion} is available";
            CurrentVersionText.Text = _updateInfo.CurrentVersion;
            LatestVersionText.Text = _updateInfo.LatestVersion;
            
            // Format release notes (convert Markdown-style formatting to plain text)
            ReleaseNotesText.Text = FormatReleaseNotes(_updateInfo.ReleaseNotes);
            
            // Set window title with version info
            this.Title = $"Update Available - {_updateInfo.ReleaseName}";
        }

        /// <summary>
        /// Format release notes for display (basic Markdown to plain text conversion)
        /// </summary>
        private string FormatReleaseNotes(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "No release notes available.";
            }

            // Basic Markdown formatting cleanup
            string formatted = markdown
                .Replace("### ", "• ")  // Convert H3 to bullets
                .Replace("## ", "• ")   // Convert H2 to bullets  
                .Replace("# ", "• ")    // Convert H1 to bullets
                .Replace("- ", "  • ")  // Convert list items
                .Replace("* ", "  • ")  // Convert list items
                .Replace("**", "")      // Remove bold markers
                .Replace("__", "")      // Remove bold markers
                .Replace("`", "")       // Remove code markers
                .Trim();

            return string.IsNullOrWhiteSpace(formatted) ? "No release notes available." : formatted;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            // Record user preference about update checks
            DisableUpdateChecks = DontAskAgainCheckBox.IsChecked ?? false;

            // Open the GitHub releases page in the browser
            _updateService.OpenReleasePage(_updateInfo.DownloadUrl);

            // Close dialog with positive result
            this.DialogResult = true;
            this.Close();
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            // Record user preference about update checks
            DisableUpdateChecks = DontAskAgainCheckBox.IsChecked ?? false;

            // Close dialog with negative result
            this.DialogResult = false;
            this.Close();
        }
    }
}