using System;
using System.Globalization;
using System.Windows.Data;

namespace AstroImages.Wpf.Converters
{
    public class MedianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double median)
            {
                return $"{median:F4}";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
