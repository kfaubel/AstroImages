using System;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn, 
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval)
        {
            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentTheme, currentShowFullScreenHelp, currentPlayPauseInterval);
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.SelectedTheme, dialog.ShowFullScreenHelp, dialog.PlayPauseInterval);
            }
            return (null, null, null, null);
        }
    }
}
