using System;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public (bool? showSizeColumn, ThemeMode? theme) ShowGeneralOptionsDialog(bool currentShowSizeColumn, ThemeMode currentTheme)
        {
            var dialog = new GeneralOptionsDialog(currentShowSizeColumn, currentTheme);
            if (dialog.ShowDialog() == true)
            {
                return (dialog.ShowSizeColumn, dialog.SelectedTheme);
            }
            return (null, null);
        }
    }
}
