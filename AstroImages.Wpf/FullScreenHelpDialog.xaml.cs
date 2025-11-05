using System.Windows;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Help dialog for full screen mode
    /// </summary>
    public partial class FullScreenHelpDialog : Window
    {
        private readonly AppConfig _appConfig;

        public FullScreenHelpDialog(AppConfig appConfig)
        {
            InitializeComponent();
            _appConfig = appConfig;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Save the preference if checkbox is checked
            if (DontShowAgainCheckBox.IsChecked == true)
            {
                _appConfig.ShowFullScreenHelp = false;
                _appConfig.Save();
            }
            
            Close();
        }
    }
}
