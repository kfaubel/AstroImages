using System;
using System.Collections.Generic;
using System.Linq;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, bool? showMedianColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval, bool? scanXisfForFitsKeywords, MedianDisplayMode? medianDisplayMode, bool? showHistogram, List<string>? fitsKeywords, List<string>? customKeywords) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn,
            bool currentShowMedianColumn,
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval,
            bool currentScanXisfForFitsKeywords,
            MedianDisplayMode currentMedianDisplayMode,
            bool currentShowHistogram,
            IEnumerable<string> currentFitsKeywords,
            IEnumerable<string> currentCustomKeywords)
        {
            // Track keyword changes from sub-dialogs
            List<string>? newFitsKeywords = null;
            List<string>? newCustomKeywords = null;

            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentShowMedianColumn, currentTheme, currentShowFullScreenHelp, currentPlayPauseInterval, currentScanXisfForFitsKeywords, currentMedianDisplayMode, currentShowHistogram);
            
            // Wire up the FITS Keywords button
            dialog.FitsKeywordsRequested += () =>
            {
                var fitsDialog = new FitsKeywordsDialog(currentFitsKeywords);
                fitsDialog.Owner = dialog;
                if (fitsDialog.ShowDialog() == true)
                {
                    newFitsKeywords = fitsDialog.GetSelectedKeywords();
                    // Update currentFitsKeywords for any subsequent opens
                    currentFitsKeywords = newFitsKeywords;
                }
            };
            
            // Wire up the Custom Keywords button
            dialog.CustomKeywordsRequested += () =>
            {
                var customDialog = new CustomKeywordsDialog(currentCustomKeywords);
                customDialog.Owner = dialog;
                if (customDialog.ShowDialog() == true)
                {
                    newCustomKeywords = customDialog.Keywords.ToList();
                    // Update currentCustomKeywords for any subsequent opens
                    currentCustomKeywords = newCustomKeywords;
                }
            };
            
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.ShowMedianColumn, dialog.SelectedTheme, dialog.ShowFullScreenHelp, dialog.PlayPauseInterval, dialog.ScanXisfForFitsKeywords, dialog.MedianDisplayMode, dialog.ShowHistogram, newFitsKeywords, newCustomKeywords);
            }
            return (null, null, null, null, null, null, null, null, null, null);
        }
    }
}
