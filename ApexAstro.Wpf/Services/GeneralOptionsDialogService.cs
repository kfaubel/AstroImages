using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexAstro.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, bool? showFullFilename, bool? showFilenameDateColumn, bool? showFilenameTimeColumn, bool? showFilenameSequenceColumn, bool? showMedianColumn, ThemeMode? theme, bool? showFullScreenHelp, double? playPauseInterval, bool? scanXisfForFitsKeywords, MedianDisplayMode? medianDisplayMode, bool? showHistogram, List<string>? fitsKeywords, List<string>? customKeywords, List<string>? csvKeywords) ShowGeneralOptionsDialog(
            bool currentShowSizeColumn,
            bool currentShowFullFilename,
            bool currentShowFilenameDateColumn,
            bool currentShowFilenameTimeColumn,
            bool currentShowFilenameSequenceColumn,
            bool currentShowMedianColumn,
            ThemeMode currentTheme, 
            bool currentShowFullScreenHelp,
            double currentPlayPauseInterval,
            bool currentScanXisfForFitsKeywords,
            MedianDisplayMode currentMedianDisplayMode,
            bool currentShowHistogram,
            IEnumerable<string> currentFitsKeywords,
            IEnumerable<string> currentCustomKeywords,
            IEnumerable<string> currentCsvKeywords)
        {
            // Track keyword changes from sub-dialogs
            List<string>? newFitsKeywords = null;
            List<string>? newCustomKeywords = null;
            List<string>? newCsvKeywords = null;

            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentShowFullFilename, currentShowFilenameDateColumn, currentShowFilenameTimeColumn, currentShowFilenameSequenceColumn, currentShowMedianColumn, currentTheme, currentShowFullScreenHelp, currentPlayPauseInterval, currentScanXisfForFitsKeywords, currentMedianDisplayMode, currentShowHistogram);
            
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

            // Wire up the CSV Keywords button
            dialog.CsvKeywordsRequested += () =>
            {
                var csvDialog = new CsvKeywordsDialog(currentCsvKeywords);
                csvDialog.Owner = dialog;
                if (csvDialog.ShowDialog() == true)
                {
                    newCsvKeywords = csvDialog.GetSelectedKeywords();
                    currentCsvKeywords = newCsvKeywords;
                }
            };
            
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.ShowFullFilename, dialog.ShowFilenameDateColumn, dialog.ShowFilenameTimeColumn, dialog.ShowFilenameSequenceColumn, dialog.ShowMedianColumn, dialog.SelectedTheme, dialog.ShowFullScreenHelp, dialog.PlayPauseInterval, dialog.ScanXisfForFitsKeywords, dialog.MedianDisplayMode, dialog.ShowHistogram, newFitsKeywords, newCustomKeywords, newCsvKeywords);
            }
            return (null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }
}
