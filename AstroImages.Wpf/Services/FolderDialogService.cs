using System;
using System.Windows;

namespace AstroImages.Wpf.Services
{
    /// <summary>
    /// Service class that handles showing folder selection dialogs to the user.
    /// Implements IFolderDialogService interface for dependency injection and testability.
    /// This separates UI concerns from business logic - the ViewModel doesn't need to know
    /// about specific dialog implementations.
    /// 
    /// Uses Windows.Forms.OpenFileDialog with folder selection mode, which better
    /// respects the system theme compared to FolderBrowserDialog.
    /// </summary>
    public class FolderDialogService : IFolderDialogService
    {
        /// <summary>
        /// Shows a folder browser dialog and returns the selected folder path.
        /// Uses OpenFileDialog in folder selection mode, which respects system theme better.
        /// </summary>
        /// <param name="initialDirectory">The directory to start browsing from</param>
        /// <returns>The selected folder path, or null if user cancelled</returns>
        public string? ShowFolderDialog(string initialDirectory)
        {
            try
            {
                // Use OpenFileDialog with ValidateNames=false to allow folder selection
                // This respects the system theme better than FolderBrowserDialog
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    dialog.Title = "Select Folder";
                    dialog.InitialDirectory = initialDirectory;
                    
                    // These settings make it act like a folder picker
                    dialog.CheckFileExists = false;
                    dialog.CheckPathExists = true;
                    dialog.ValidateNames = false;
                    dialog.FileName = "Select Folder";
                    
                    // Get the window handle for the current WPF window
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                    
                    // Show the dialog
                    var result = dialog.ShowDialog();
                    
                    // Check if user clicked OK
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        // Get the folder path
                        string? pathDir = System.IO.Path.GetDirectoryName(dialog.FileName);
                        if (!string.IsNullOrEmpty(pathDir) && System.IO.Directory.Exists(pathDir))
                        {
                            return pathDir;
                        }
                    }
                    
                    // User cancelled
                    return null;
                }
            }
            catch
            {
                // Fallback to the legacy folder browser dialog if OpenFileDialog fails
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.SelectedPath = initialDirectory;
                    dlg.ShowNewFolderButton = true;
                    
                    var result = dlg.ShowDialog();
                    
                    if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                    {
                        return dlg.SelectedPath;
                    }
                    
                    return null;
                }
            }
        }
    }
}
