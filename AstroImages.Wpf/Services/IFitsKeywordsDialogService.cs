using System;
using System.Collections.Generic;

namespace AstroImages.Wpf.Services
{
    public interface IFitsKeywordsDialogService
    {
        /// <summary>
        /// Shows the FITS Keywords dialog and returns the new keyword list if changed, or null if cancelled.
        /// </summary>
        /// <param name="currentKeywords">The current list of FITS keywords.</param>
        /// <returns>The new list of FITS keywords, or null if cancelled.</returns>
        List<string>? ShowFitsKeywordsDialog(IEnumerable<string> currentKeywords);
    }
}
