using System;
using System.Windows;

namespace AstroImages.Wpf.Services
{
    /// <summary>
    /// Service class that handles showing folder selection dialogs to the user.
    /// Implements IFolderDialogService interface for dependency injection and testability.
    /// This separates UI concerns from business logic - the ViewModel doesn't need to know
    /// about specific dialog implementations.
    /// </summary>
    public class FolderDialogService : IFolderDialogService
    {
        /// <summary>
        /// Shows a folder browser dialog and returns the selected folder path.
        /// </summary>
        /// <param name="initialDirectory">The directory to start browsing from</param>
        /// <returns>The selected folder path, or null if user cancelled</returns>
        public string? ShowFolderDialog(string initialDirectory)
        {
            // Create a Windows Forms folder browser dialog
            // We use Windows Forms here because WPF doesn't have a built-in folder dialog
            // "using" ensures the dialog is properly disposed when done
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                // Set the starting directory for the dialog
                dlg.SelectedPath = initialDirectory;
                
                // Allow user to create new folders in the dialog
                dlg.ShowNewFolderButton = true;
                
                // Show the dialog and get the result
                // This blocks until user clicks OK or Cancel
                var result = dlg.ShowDialog();
                
                // Check if user clicked OK or Yes (both mean they selected a folder)
                if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                {
                    // Return the path the user selected
                    return dlg.SelectedPath;
                }
                
                // User cancelled or closed the dialog
                return null;
            }
        }
    }
}
