using System;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval, bool? scanXisfForFitsKeywords, int? stretchAggressiveness) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn, 
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval,
            bool currentScanXisfForFitsKeywords,
            int currentStretchAggressiveness)
        {
            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentTheme, currentShowFullScreenHelp, currentPlayPauseInterval, currentScanXisfForFitsKeywords, currentStretchAggressiveness);
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.SelectedTheme, dialog.ShowFullScreenHelp, dialog.PlayPauseInterval, dialog.ScanXisfForFitsKeywords, dialog.StretchAggressiveness);
            }
            return (null, null, null, null, null, null);
        }
    }
}
