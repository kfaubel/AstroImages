using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace AstroImages.Wpf.Converters
{
    /// <summary>
    /// Converter to safely access dictionary values without throwing exceptions when keys don't exist.
    /// Returns empty string for missing keys instead of throwing KeyNotFoundException.
    /// Uses the parameter as the dictionary key.
    /// </summary>
    public class SafeDictionaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string key)
            {
                // Handle Dictionary<string, string> (used by FileItem)
                if (value is Dictionary<string, string> stringDict)
                {
                    if (stringDict.TryGetValue(key, out var stringValue))
                    {
                        return stringValue ?? "";
                    }
                }
                // Handle Dictionary<string, object> (used elsewhere)
                else if (value is Dictionary<string, object> objectDict)
                {
                    if (objectDict.TryGetValue(key, out var objectValue))
                    {
                        return objectValue?.ToString() ?? "";
                    }
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}