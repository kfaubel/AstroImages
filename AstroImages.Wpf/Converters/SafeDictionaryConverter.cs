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
    /// 
    /// Special handling:
    /// - RMS: Shows "---" if value is "0" or "0.0"
    /// - ECC: Shows "---" if value is "NaN" (case-insensitive)
    /// </summary>
    public class SafeDictionaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string key)
            {
                string? displayValue = null;

                // Handle Dictionary<string, string> (used by FileItem)
                if (value is Dictionary<string, string> stringDict)
                {
                    if (stringDict.TryGetValue(key, out var stringValue))
                    {
                        displayValue = stringValue;
                    }
                }
                // Handle Dictionary<string, object> (used elsewhere)
                else if (value is Dictionary<string, object> objectDict)
                {
                    if (objectDict.TryGetValue(key, out var objectValue))
                    {
                        displayValue = objectValue?.ToString();
                    }
                }

                // If we have a value, apply special formatting rules
                if (displayValue != null)
                {
                    // Special handling for RMS: show "---" if value is 0
                    if (key.Equals("RMS", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(displayValue, out double rmsValue))
                        {
                            if (rmsValue == 0.0)
                            {
                                return "---";
                            }
                        }
                        return displayValue;
                    }

                    // Special handling for ECC: show "---" if value is NaN
                    if (key.Equals("ECC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (displayValue.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                        {
                            return "---";
                        }
                        return displayValue;
                    }

                    return displayValue;
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