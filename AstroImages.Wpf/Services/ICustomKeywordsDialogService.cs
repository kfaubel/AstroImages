using System;

namespace AstroImages.Wpf.Services
{
    public interface ICustomKeywordsDialogService
    {
        /// <summary>
        /// Shows the Custom Keywords dialog and returns the new keyword list if changed, or null if cancelled.
        /// </summary>
        /// <param name="currentKeywords">The current list of custom keywords.</param>
        /// <returns>The new list of custom keywords, or null if cancelled.</returns>
        System.Collections.Generic.List<string>? ShowCustomKeywordsDialog(System.Collections.Generic.IEnumerable<string> currentKeywords);
    }
}
