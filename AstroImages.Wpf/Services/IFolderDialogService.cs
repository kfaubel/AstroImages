using System;

namespace AstroImages.Wpf.Services
{
    public interface IFolderDialogService
    {
        /// <summary>
        /// Shows a folder selection dialog and returns the selected path, or null if cancelled.
        /// </summary>
        /// <param name="initialDirectory">The initial directory to open in the dialog.</param>
        /// <returns>The selected folder path, or null if cancelled.</returns>
        string? ShowFolderDialog(string initialDirectory);
    }
}
