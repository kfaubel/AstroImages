using System;
using System.Collections.Generic;

namespace AstroImages.Wpf.Services
{
    public class CustomKeywordsDialogService : ICustomKeywordsDialogService
    {
        public List<string>? ShowCustomKeywordsDialog(IEnumerable<string> currentKeywords)
        {
            var dialog = new CustomKeywordsDialog(currentKeywords);
            if (dialog.ShowDialog() == true)
            {
                return dialog.Keywords.ToList();
            }
            return null;
        }
    }
}
