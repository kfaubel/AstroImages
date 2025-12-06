using System;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval, bool? scanXisfForFitsKeywords) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn, 
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval,
            bool currentScanXisfForFitsKeywords)
        {
            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentTheme, currentShowFullScreenHelp, currentPlayPauseInterval, currentScanXisfForFitsKeywords);
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.SelectedTheme, dialog.ShowFullScreenHelp, dialog.PlayPauseInterval, dialog.ScanXisfForFitsKeywords);
            }
            return (null, null, null, null, null);
        }
    }
}
