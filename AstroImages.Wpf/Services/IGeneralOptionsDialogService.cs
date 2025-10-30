using System;

namespace AstroImages.Wpf.Services
{
    public interface IGeneralOptionsDialogService
    {
        /// <summary>
        /// Shows the General Options dialog and returns the new ShowSizeColumn value if changed, or null if cancelled.
        /// </summary>
        /// <param name="currentShowSizeColumn">The current ShowSizeColumn value.</param>
        /// <returns>The new ShowSizeColumn value, or null if cancelled.</returns>
        bool? ShowGeneralOptionsDialog(bool currentShowSizeColumn);
    }
}
