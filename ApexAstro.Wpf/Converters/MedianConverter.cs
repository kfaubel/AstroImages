using System;
using System.Globalization;
using System.Windows.Data;

namespace ApexAstro.Wpf.Converters
{
    public class MedianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double median)
            {
                // Get the display mode from AppConfig passed as parameter
                if (parameter is AppConfig appConfig)
                {
                    if (appConfig.MedianDisplayMode == MedianDisplayMode.SixteenBit)
                    {
                        // Convert to 16-bit range (0-65535)
                        int sixteenBitValue = (int)Math.Round(median * 65535.0);
                        return sixteenBitValue.ToString();
                    }
                }
                
                // Default: normalized (0.0-1.0) with 5 decimal places for consistent width
                return median.ToString("F5", culture);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
