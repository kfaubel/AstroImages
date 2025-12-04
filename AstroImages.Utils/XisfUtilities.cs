using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AstroImages.Utils
{
    /// <summary>
    /// Utility functions for XISF (Extensible Image Serialization Format) file operations.
    /// Provides enhanced functionality for working with XISF files beyond basic parsing.
    /// </summary>
    public static class XisfUtilities
    {
        private const string XISF_SIGNATURE = "XISF0100";
        
        /// <summary>
        /// Check if a file is an XISF file by examining the header signature
        /// </summary>
        public static bool IsXisfFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[8];
                
                if (fileStream.Read(buffer, 0, 8) == 8)
                {
                    var signature = Encoding.ASCII.GetString(buffer);
                    return signature == XISF_SIGNATURE;
                }
            }
            catch
            {
                // If we can't read the file, assume it's not an XISF file
            }

            return false;
        }

        /// <summary>
        /// Check if the file extension suggests it might be an XISF file
        /// </summary>
        public static bool HasXisfExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
                
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".xisf";
        }

        /// <summary>
        /// Validate XISF data structure in memory
        /// </summary>
        public static bool IsXisfData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 8)
                return false;

            var signature = Encoding.ASCII.GetString(buffer, 0, 8);
            return signature == XISF_SIGNATURE;
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
        /// Extract astronomical metadata from XISF properties
        /// </summary>
        public static Dictionary<string, object> ExtractAstronomicalMetadata(Dictionary<string, object> properties)
        {
            var metadata = new Dictionary<string, object>();
            
            // Look for common astronomical properties in XISF namespace format
            var astroPatterns = new[]
            {
                // XISF Astronomical namespaces
                "Observation_Object_Name", "Observation_Object_RA", "Observation_Object_Dec",
                "Observation_Center_RA", "Observation_Center_Dec",
                "Observation_Time_Start", "Observation_Time_End",
                "Observation_Location_Name", "Observation_Location_Latitude", "Observation_Location_Longitude",
                "Instrument_Camera_Name", "Instrument_Telescope_Name",
                "Instrument_ExposureTime", "Instrument_Filter_Name",
                "Instrument_Camera_Gain", "Instrument_Camera_ReadoutNoise",
                "Instrument_Camera_XBinning", "Instrument_Camera_YBinning",
                "Instrument_Sensor_Temperature", "Instrument_Focuser_Position",
                
                // Legacy FITS keywords embedded in XISF
                "FITS_OBJECT", "FITS_FILTER", "FITS_EXPTIME", "FITS_CCD-TEMP",
                "FITS_GAIN", "FITS_OFFSET", "FITS_XBINNING", "FITS_YBINNING",
                "FITS_IMAGETYP", "FITS_FRAME", "FITS_DATE-OBS", "FITS_TIME-OBS",
                "FITS_RA", "FITS_DEC", "FITS_AIRMASS", "FITS_FOCPOS",
                "FITS_INSTRUME", "FITS_TELESCOP", "FITS_OBSERVER"
            };

            foreach (var pattern in astroPatterns)
            {
                if (properties.TryGetValue(pattern, out var value))
                {
                    metadata[pattern] = value;
                }
            }

            return metadata;
        }

        /// <summary>
        /// Get format information from XISF properties
        /// </summary>
        public static Dictionary<string, object> GetFormatInfo(Dictionary<string, object> properties)
        {
            var formatInfo = new Dictionary<string, object>();

            // Basic image format information
            if (properties.TryGetValue("Image_geometry", out var geometry))
                formatInfo["Geometry"] = geometry;
            
            if (properties.TryGetValue("Image_sampleFormat", out var sampleFormat))
            {
                formatInfo["SampleFormat"] = sampleFormat;
                formatInfo["DataType"] = GetDataTypeDescription(sampleFormat.ToString() ?? "");
            }
            
            if (properties.TryGetValue("Image_Width", out var width))
                formatInfo["Width"] = width;
            
            if (properties.TryGetValue("Image_Height", out var height))
                formatInfo["Height"] = height;
                
            if (properties.TryGetValue("Image_Channels", out var channels))
                formatInfo["Channels"] = channels;

            if (properties.TryGetValue("Image_colorSpace", out var colorSpace))
                formatInfo["ColorSpace"] = colorSpace;
                
            if (properties.TryGetValue("Image_bounds", out var bounds))
                formatInfo["Bounds"] = bounds;
                
            if (properties.TryGetValue("Image_LowerBound", out var lowerBound))
                formatInfo["LowerBound"] = lowerBound;
                
            if (properties.TryGetValue("Image_UpperBound", out var upperBound))
                formatInfo["UpperBound"] = upperBound;

            return formatInfo;
        }

        /// <summary>
        /// Format an XISF property value for display
        /// </summary>
        public static string FormatPropertyValue(object value)
        {
            if (value == null)
                return "";

            return value switch
            {
                bool b => b ? "True" : "False",
                double d => FormatNumber(d),
                float f => FormatNumber(f),
                int i => i.ToString(),
                uint ui => ui.ToString(),
                long l => l.ToString(),
                ulong ul => ul.ToString(),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                string s => TruncateAndCleanString(s),
                _ => TruncateAndCleanString(value.ToString())
            };
        }

        /// <summary>
        /// Format a numeric value, showing integers without decimal point and 
        /// floating point values rounded to 5 decimal places
        /// </summary>
        private static string FormatNumber(double value)
        {
            var rounded = Math.Round(value, 5);
            
            // If the rounded value equals its integer conversion, it's a whole number
            if (rounded == Math.Floor(rounded))
            {
                return ((long)rounded).ToString();
            }
            
            return rounded.ToString("G");
        }

        /// <summary>
        /// Clean and truncate string values for display in metadata dialogs
        /// Removes newlines and limits to 80 characters for single-line display
        /// </summary>
        private static string TruncateAndCleanString(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // Remove all newlines and replace with spaces, then trim
            var cleaned = input.Replace('\r', ' ').Replace('\n', ' ').Trim();
            
            // Collapse multiple spaces into single spaces
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }
            
            // Limit to 80 characters for single-line display
            if (cleaned.Length > 80)
            {
                cleaned = cleaned.Substring(0, 77) + "...";
            }
            
            return cleaned;
        }

        /// <summary>
        /// Get a description of the XISF data type
        /// </summary>
        private static string GetDataTypeDescription(string sampleFormat)
        {
            return sampleFormat.ToUpperInvariant() switch
            {
                "UINT8" => "8-bit unsigned integer",
                "UINT16" => "16-bit unsigned integer", 
                "UINT32" => "32-bit unsigned integer",
                "UINT64" => "64-bit unsigned integer",
                "FLOAT32" => "32-bit floating point",
                "FLOAT64" => "64-bit floating point",
                "COMPLEX32" => "32-bit complex floating point",
                "COMPLEX64" => "64-bit complex floating point",
                "" => "Unknown",
                _ => $"Unknown ({sampleFormat})"
            };
        }

        /// <summary>
        /// Extract metadata organization for display
        /// </summary>
        public static Dictionary<string, object> ExtractMetadataInfo(Dictionary<string, object> properties)
        {
            var metadataInfo = new Dictionary<string, object>();

            // XISF Metadata properties (with XISF: namespace)
            var metadataKeys = new[]
            {
                "Metadata_XISF_CreationTime", "Metadata_XISF_CreatorApplication",
                "Metadata_XISF_CreatorModule", "Metadata_XISF_CreatorOS",
                "Metadata_XISF_Title", "Metadata_XISF_Description", "Metadata_XISF_Keywords",
                "Metadata_XISF_Authors", "Metadata_XISF_Copyright",
                "Metadata_XISF_CompressionCodecs", "Metadata_XISF_CompressionLevel"
            };

            foreach (var key in metadataKeys)
            {
                if (properties.TryGetValue(key, out var value))
                {
                    metadataInfo[key.Replace("Metadata_", "")] = value;
                }
            }

            return metadataInfo;
        }

        /// <summary>
        /// Validate XISF structure and properties
        /// </summary>
        public static List<string> ValidateXisfStructure(Dictionary<string, object> properties)
        {
            var issues = new List<string>();

            // Check for essential image properties
            if (!properties.ContainsKey("Image_geometry"))
            {
                issues.Add("Missing image geometry information");
            }

            if (!properties.ContainsKey("Image_sampleFormat"))
            {
                issues.Add("Missing sample format information");
            }

            // Validate geometry format
            if (properties.TryGetValue("Image_geometry", out var geometry))
            {
                var geometryStr = geometry.ToString();
                if (string.IsNullOrEmpty(geometryStr) || geometryStr.Split(':').Length < 3)
                {
                    issues.Add($"Invalid geometry format: {geometryStr}");
                }
            }

            // Validate sample format
            if (properties.TryGetValue("Image_sampleFormat", out var sampleFormat))
            {
                var formatStr = sampleFormat.ToString()?.ToUpperInvariant();
                var validFormats = new[] { "UINT8", "UINT16", "UINT32", "UINT64", "FLOAT32", "FLOAT64", "COMPLEX32", "COMPLEX64" };
                if (!string.IsNullOrEmpty(formatStr) && !Array.Exists(validFormats, f => f == formatStr))
                {
                    issues.Add($"Unsupported sample format: {formatStr}");
                }
            }

            // Check for required bounds on floating point images
            if (properties.TryGetValue("Image_sampleFormat", out var format))
            {
                var formatStr = format.ToString()?.ToUpperInvariant();
                if (formatStr != null && (formatStr.StartsWith("FLOAT") || formatStr.StartsWith("COMPLEX")))
                {
                    if (!properties.ContainsKey("Image_bounds"))
                    {
                        issues.Add("Missing bounds information for floating point image");
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Extract processing history from XISF properties
        /// </summary>
        public static List<string> ExtractProcessingHistory(Dictionary<string, object> properties)
        {
            var history = new List<string>();

            // Look for Processing:History property
            if (properties.TryGetValue("Processing_History", out var historyValue))
            {
                var historyStr = historyValue.ToString();
                if (!string.IsNullOrEmpty(historyStr))
                {
                    // Split by newlines to get individual history entries
                    var entries = historyStr.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    history.AddRange(entries);
                }
            }

            return history;
        }

        /// <summary>
        /// Get XISF version information from properties
        /// </summary>
        public static string GetXisfVersion(Dictionary<string, object> properties)
        {
            if (properties.TryGetValue("XISF_Version", out var version))
            {
                return version.ToString() ?? "Unknown";
            }
            return "1.0"; // Default XISF version supported
        }

        /// <summary>
        /// Check if XISF file contains compressed data
        /// </summary>
        public static bool IsCompressed(Dictionary<string, object> properties)
        {
            // Check for compression codec properties
            return properties.ContainsKey("Metadata_XISF_CompressionCodecs") ||
                   properties.ContainsKey("Image_compression");
        }

        /// <summary>
        /// Get supported XISF file extensions
        /// </summary>
        public static string[] GetSupportedExtensions()
        {
            return new[] { ".xisf" };
        }
    }
}