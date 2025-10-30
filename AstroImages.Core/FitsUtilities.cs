using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AstroImages.Core
{
    /// <summary>
    /// Utility functions for FITS file operations and astronomical metadata processing.
    /// Provides enhanced functionality for working with FITS files beyond basic parsing.
    /// </summary>
    public static class FitsUtilities
    {
        /// <summary>
        /// Check if a file is a FITS file by examining the header
        /// </summary>
        public static bool IsFitsFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[80];
                
                if (fileStream.Read(buffer, 0, 80) == 80)
                {
                    var firstCard = System.Text.Encoding.ASCII.GetString(buffer);
                    return firstCard.StartsWith("SIMPLE  =") || firstCard.StartsWith("XTENSION=");
                }
            }
            catch
            {
                // If we can't read the file, assume it's not a FITS file
            }

            return false;
        }

        /// <summary>
        /// Validate FITS data structure in memory
        /// </summary>
        public static bool IsFitsData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 80)
                return false;

            var firstCard = System.Text.Encoding.ASCII.GetString(buffer, 0, 80);
            return firstCard.StartsWith("SIMPLE  =") || firstCard.StartsWith("XTENSION=");
        }

        /// <summary>
        /// Calculate basic image statistics from pixel data
        /// </summary>
        public static Dictionary<string, double> CalculateImageStatistics(byte[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return new Dictionary<string, double>();

            var stats = new Dictionary<string, double>();
            
            // Calculate basic statistics
            double sum = 0;
            byte min = byte.MaxValue;
            byte max = byte.MinValue;
            
            foreach (byte pixel in pixels)
            {
                sum += pixel;
                if (pixel < min) min = pixel;
                if (pixel > max) max = pixel;
            }
            
            stats["Count"] = pixels.Length;
            stats["Sum"] = sum;
            stats["Mean"] = sum / pixels.Length;
            stats["Min"] = min;
            stats["Max"] = max;
            
            // Calculate standard deviation
            double sumSquaredDiffs = 0;
            double mean = stats["Mean"];
            foreach (byte pixel in pixels)
            {
                double diff = pixel - mean;
                sumSquaredDiffs += diff * diff;
            }
            
            stats["StdDev"] = Math.Sqrt(sumSquaredDiffs / pixels.Length);
            
            return stats;
        }

        /// <summary>
        /// Extract common astronomical metadata from FITS headers
        /// </summary>
        public static Dictionary<string, object> ExtractAstronomicalMetadata(Dictionary<string, object> headers)
        {
            var metadata = new Dictionary<string, object>();
            
            // Common astronomical keywords to extract
            var astroKeywords = new[]
            {
                "OBJECT",     // Object name
                "FILTER",     // Filter used
                "EXPTIME",    // Exposure time
                "CCD-TEMP",   // CCD temperature
                "GAIN",       // CCD gain
                "OFFSET",     // CCD offset
                "BINNING",    // Binning factor
                "XBINNING",   // X binning
                "YBINNING",   // Y binning
                "IMAGETYP",   // Image type (Light, Dark, Bias, Flat)
                "FRAME",      // Frame type
                "DATE-OBS",   // Observation date
                "TIME-OBS",   // Observation time
                "JD",         // Julian date
                "AIRMASS",    // Airmass
                "FOCPOS",     // Focuser position
                "ROTANG",     // Rotator angle
                "PIER-SIDE",  // Pier side
                "RA",         // Right ascension
                "DEC",        // Declination
                "EQUINOX",    // Coordinate equinox
                "RADECSYS",   // Coordinate system
                "CTYPE1",     // WCS coordinate type 1
                "CTYPE2",     // WCS coordinate type 2
                "CRVAL1",     // WCS reference value 1
                "CRVAL2",     // WCS reference value 2
                "CRPIX1",     // WCS reference pixel 1
                "CRPIX2",     // WCS reference pixel 2
                "CDELT1",     // WCS pixel scale 1
                "CDELT2",     // WCS pixel scale 2
                "INSTRUME",   // Instrument name
                "TELESCOP",   // Telescope name
                "OBSERVER",   // Observer name
                "NOTES",      // Observation notes
                "WEATHER",    // Weather conditions
                "SEEING",     // Seeing conditions
                "MOONPH",     // Moon phase
                "SKYLEVEL",   // Sky level
                "FWHM",       // Full width half maximum
                "ECCENTRICITY", // Star eccentricity
                "PEAKSTAR",   // Peak star value
                "MEDIANSTAR", // Median star value
            };

            foreach (var keyword in astroKeywords)
            {
                if (headers.TryGetValue(keyword, out var value))
                {
                    metadata[keyword] = value;
                }
            }

            return metadata;
        }

        /// <summary>
        /// Get format information from FITS headers
        /// </summary>
        public static Dictionary<string, object> GetFormatInfo(Dictionary<string, object> headers)
        {
            var formatInfo = new Dictionary<string, object>();

            // Basic format information
            if (headers.TryGetValue("BITPIX", out var bitpix))
                formatInfo["BitPix"] = bitpix;
            
            if (headers.TryGetValue("NAXIS", out var naxis))
                formatInfo["Dimensions"] = naxis;
            
            if (headers.TryGetValue("NAXIS1", out var naxis1))
                formatInfo["Width"] = naxis1;
            
            if (headers.TryGetValue("NAXIS2", out var naxis2))
                formatInfo["Height"] = naxis2;

            if (headers.TryGetValue("BZERO", out var bzero))
                formatInfo["BZero"] = bzero;
            
            if (headers.TryGetValue("BSCALE", out var bscale))
                formatInfo["BScale"] = bscale;

            // Calculate data type description
            if (formatInfo.TryGetValue("BitPix", out var bitpixValue))
            {
                var bitpixInt = Convert.ToInt32(bitpixValue);
                formatInfo["DataType"] = GetDataTypeDescription(bitpixInt);
            }

            return formatInfo;
        }

        /// <summary>
        /// Format a FITS header value for display
        /// </summary>
        public static string FormatHeaderValue(object value)
        {
            if (value == null)
                return "";

            return value switch
            {
                bool b => b ? "T" : "F",
                double d => d.ToString("G"),
                float f => f.ToString("G"),
                int i => i.ToString(),
                long l => l.ToString(),
                string s => s.Trim(),
                _ => value.ToString()?.Trim() ?? ""
            };
        }

        /// <summary>
        /// Get a description of the FITS data type from BITPIX value
        /// </summary>
        private static string GetDataTypeDescription(int bitpix)
        {
            return bitpix switch
            {
                8 => "8-bit unsigned integer",
                16 => "16-bit signed integer",
                32 => "32-bit signed integer",
                -32 => "32-bit floating point",
                -64 => "64-bit floating point",
                _ => $"Unknown (BITPIX={bitpix})"
            };
        }

        /// <summary>
        /// Extract World Coordinate System (WCS) information from headers
        /// </summary>
        public static Dictionary<string, object> ExtractWcsInfo(Dictionary<string, object> headers)
        {
            var wcsInfo = new Dictionary<string, object>();

            // WCS keywords
            var wcsKeywords = new[]
            {
                "CTYPE1", "CTYPE2",   // Coordinate types
                "CRVAL1", "CRVAL2",   // Reference values
                "CRPIX1", "CRPIX2",   // Reference pixels
                "CDELT1", "CDELT2",   // Pixel scales
                "CROTA1", "CROTA2",   // Rotation angles
                "CD1_1", "CD1_2",     // Transformation matrix
                "CD2_1", "CD2_2",
                "PC1_1", "PC1_2",     // Principal components
                "PC2_1", "PC2_2",
                "EQUINOX", "RADECSYS", // Coordinate system info
                "EPOCH"               // Coordinate epoch
            };

            foreach (var keyword in wcsKeywords)
            {
                if (headers.TryGetValue(keyword, out var value))
                {
                    wcsInfo[keyword] = value;
                }
            }

            return wcsInfo;
        }

        /// <summary>
        /// Validate FITS header structure and required keywords
        /// </summary>
        public static List<string> ValidateFitsHeader(Dictionary<string, object> headers)
        {
            var issues = new List<string>();

            // Check required keywords
            var requiredKeywords = new[] { "SIMPLE", "BITPIX", "NAXIS" };
            
            foreach (var keyword in requiredKeywords)
            {
                if (!headers.ContainsKey(keyword))
                {
                    issues.Add($"Missing required keyword: {keyword}");
                }
            }

            // Validate SIMPLE keyword
            if (headers.TryGetValue("SIMPLE", out var simple))
            {
                if (simple is bool simpleBool && !simpleBool)
                {
                    issues.Add("SIMPLE keyword is not True");
                }
                else if (simple is string simpleStr && !simpleStr.Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add("SIMPLE keyword is not True");
                }
            }

            // Validate BITPIX
            if (headers.TryGetValue("BITPIX", out var bitpix))
            {
                var bitpixInt = Convert.ToInt32(bitpix);
                var validBitpix = new[] { 8, 16, 32, -32, -64 };
                if (!validBitpix.Contains(bitpixInt))
                {
                    issues.Add($"Invalid BITPIX value: {bitpixInt}");
                }
            }

            // Validate NAXIS
            if (headers.TryGetValue("NAXIS", out var naxis))
            {
                var naxisInt = Convert.ToInt32(naxis);
                if (naxisInt < 0)
                {
                    issues.Add($"Invalid NAXIS value: {naxisInt}");
                }

                // Check for corresponding NAXISn keywords
                for (int i = 1; i <= naxisInt; i++)
                {
                    if (!headers.ContainsKey($"NAXIS{i}"))
                    {
                        issues.Add($"Missing NAXIS{i} keyword");
                    }
                }
            }

            return issues;
        }
    }
}