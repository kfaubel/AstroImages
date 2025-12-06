using System;

namespace AstroImages.Wpf.Services
{
    public interface IGeneralOptionsDialogService
    {
        /// <summary>
        /// Shows the General Options dialog and returns the new settings if changed, or null if cancelled.
        /// </summary>
        /// <param name="currentShowSizeColumn">The current ShowSizeColumn value.</param>
        /// <param name="currentTheme">The current theme mode.</param>
        /// <param name="currentShowFullScreenHelp">The current ShowFullScreenHelp value.</param>
        /// <param name="currentPlayPauseInterval">The current play pause interval in seconds.</param>
        /// <param name="currentScanXisfForFitsKeywords">The current ScanXisfForFitsKeywords value.</param>
        /// <returns>A tuple with the new settings, or null values if cancelled.</returns>
        (bool? showSizeColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval, bool? scanXisfForFitsKeywords) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn, 
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval,
            bool currentScanXisfForFitsKeywords);
    }
}
