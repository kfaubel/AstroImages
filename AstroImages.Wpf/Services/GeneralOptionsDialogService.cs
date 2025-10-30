using System;

namespace AstroImages.Wpf.Services
{
    public class GeneralOptionsDialogService : IGeneralOptionsDialogService
    {
        public bool? ShowGeneralOptionsDialog(bool currentShowSizeColumn)
        {
            var dialog = new GeneralOptionsDialog(currentShowSizeColumn);
            if (dialog.ShowDialog() == true)
            {
                return dialog.ShowSizeColumn;
            }
            return null;
        }
    }
}
