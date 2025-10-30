using System;
using System.Collections.Generic;

namespace AstroImages.Wpf.Services
{
    public class FitsKeywordsDialogService : IFitsKeywordsDialogService
    {
        public List<string>? ShowFitsKeywordsDialog(IEnumerable<string> currentKeywords)
        {
            var dialog = new FitsKeywordsDialog(currentKeywords);
            if (dialog.ShowDialog() == true)
            {
                return dialog.GetSelectedKeywords();
            }
            return null;
        }
    }
}
