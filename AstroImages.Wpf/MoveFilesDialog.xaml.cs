using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace AstroImages.Wpf
{
    public partial class MoveFilesDialog : Window
    {
        public List<string> FilesToMove { get; set; } = new List<string>();
        public string TargetDirectory { get; private set; } = string.Empty;
        public bool MoveToTrash { get; private set; } = false;

        public MoveFilesDialog()
        {
            InitializeComponent();
        }

        public MoveFilesDialog(List<string> fileNames) : this()
        {
            FilesToMove = fileNames;
            // Instead of showing individual files, show just the count
            FilesListControl.ItemsSource = new[] { $"Moving {fileNames.Count} file{(fileNames.Count == 1 ? "" : "s")}..." };
        }

        public void SetDefaultDirectory(string defaultDirectory)
        {
            if (!string.IsNullOrEmpty(defaultDirectory) && Directory.Exists(defaultDirectory))
            {
                DirectoryTextBox.Text = defaultDirectory;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Use OpenFolderDialog if available (Windows 10/11), fallback to OpenFileDialog
            var dialog = new OpenFolderDialog();
            dialog.Title = "Select destination folder";
            
            // Set the initial directory to the current text box value if it exists
            if (!string.IsNullOrEmpty(DirectoryTextBox.Text) && Directory.Exists(DirectoryTextBox.Text))
            {
                dialog.InitialDirectory = DirectoryTextBox.Text;
            }

            if (dialog.ShowDialog() == true)
            {
                DirectoryTextBox.Text = dialog.FolderName;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (MoveToDirectoryRadio.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(DirectoryTextBox.Text))
                {
                    System.Windows.MessageBox.Show("Please select a destination directory.", "No Directory Selected", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Directory.Exists(DirectoryTextBox.Text))
                {
                    System.Windows.MessageBox.Show("The selected directory does not exist.", "Invalid Directory", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TargetDirectory = DirectoryTextBox.Text;
                MoveToTrash = false;
            }
            else if (MoveToTrashRadio.IsChecked == true)
            {
                MoveToTrash = true;
                TargetDirectory = string.Empty;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}