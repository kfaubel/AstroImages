using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AstroImages.Wpf.Services
{
    /// <summary>
    /// Loads and caches session metadata CSV files found in the current image folder.
    /// Supports both ImageMetaData.csv (per-file values) and AcquisitionDetails.csv
    /// (session-level values, optionally per-file if FilePath is present).
    /// </summary>
    public class CsvMetadataService
    {
        private const string ImageMetadataFileName = "ImageMetaData.csv";
        private const string AcquisitionDetailsFileName = "AcquisitionDetails.csv";

        // Column name used as the match key (the FilePath column, stripped to filename)
        private const string FilePathColumn = "FilePath";

        // Cache: filename (lowercase) -> row values from per-file metadata.
        private Dictionary<string, Dictionary<string, string>> _rowsByFilename = new();

        // Cache: session-level values from AcquisitionDetails.csv.
        private Dictionary<string, string> _sessionValues = new(StringComparer.OrdinalIgnoreCase);

        // Column names in order, excluding FilePath
        private List<string> _availableColumns = new();

        /// <summary>
        /// Known CSV column names and their human-readable descriptions.
        /// Used to populate the CsvKeywordsDialog even before a CSV is loaded.
        /// </summary>
        public static readonly List<(string Column, string Description)> KnownColumns = new()
        {
            ("ExposureNumber",      "Sequential exposure number"),
            ("FilterName",          "Filter used for this exposure"),
            ("ExposureStart",       "Local date and time exposure started"),
            ("ExposureStartUTC",    "UTC date and time exposure started"),
            ("Duration",            "Exposure duration in seconds"),
            ("Binning",             "Camera binning (e.g., 1x1, 2x2)"),
            ("CameraTemp",          "Camera temperature in °C"),
            ("CameraTargetTemp",    "Camera target temperature in °C"),
            ("Gain",                "Camera gain setting"),
            ("Offset",              "Camera offset setting"),
            ("ADUStDev",            "ADU standard deviation"),
            ("ADUMean",             "ADU mean value"),
            ("ADUMedian",           "ADU median value"),
            ("ADUMin",              "ADU minimum value"),
            ("ADUMax",              "ADU maximum value"),
            ("DetectedStars",       "Number of stars detected"),
            ("HFR",                 "Half flux radius of stars"),
            ("HFRStDev",            "HFR standard deviation"),
            ("FWHM",                "Full width at half maximum (arcsec)"),
            ("Eccentricity",        "Star eccentricity (0=round, 1=elongated)"),
            ("GuidingRMS",          "Total guiding RMS (pixels)"),
            ("GuidingRMSArcSec",    "Total guiding RMS (arcsec)"),
            ("GuidingRMSRA",        "Guiding RMS in RA axis (pixels)"),
            ("GuidingRMSRAArcSec",  "Guiding RMS in RA axis (arcsec)"),
            ("GuidingRMSDEC",       "Guiding RMS in DEC axis (pixels)"),
            ("GuidingRMSDECArcSec", "Guiding RMS in DEC axis (arcsec)"),
            ("FocuserPosition",     "Focuser position (steps)"),
            ("FocuserTemp",         "Focuser temperature in °C"),
            ("RotatorPosition",     "Rotator angle in degrees"),
            ("PierSide",            "Telescope pier side (East/West)"),
            ("Airmass",             "Airmass at frame center"),
            ("MountRA",             "Mount right ascension in degrees"),
            ("MountDec",            "Mount declination in degrees"),
            ("ImageType",           "Image type (LIGHT, DARK, FLAT, BIAS)"),

            // AcquisitionDetails.csv session-level columns
            ("TargetName",          "Target object name"),
            ("RACoordinates",       "Target right ascension"),
            ("DECCoordinates",      "Target declination"),
            ("TelescopeName",       "Telescope name"),
            ("FocalLength",         "Telescope focal length"),
            ("FocalRatio",          "Telescope focal ratio"),
            ("CameraName",          "Camera model name"),
            ("PixelSize",           "Camera pixel size"),
            ("BitDepth",            "Camera bit depth"),
            ("ObserverLatitude",    "Observer latitude"),
            ("ObserverLongitude",   "Observer longitude"),
            ("ObserverElevation",   "Observer elevation"),
        };

        /// <summary>
        /// Loads session metadata CSV files from the specified folder.
        /// Clears any previously loaded data first.
        /// Each file is optional.
        /// </summary>
        public void Load(string folderPath)
        {
            _rowsByFilename = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _sessionValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _availableColumns = new List<string>();

            LoadImageMetadata(folderPath);
            LoadAcquisitionDetails(folderPath);
        }

        private void LoadImageMetadata(string folderPath)
        {
            var csvPath = Path.Combine(folderPath, ImageMetadataFileName);
            if (!File.Exists(csvPath))
                return;

            try
            {
                using var reader = new StreamReader(csvPath);
                var headerLine = reader.ReadLine();
                if (headerLine == null) return;

                var headers = ParseCsvLine(headerLine);
                var filePathIndex = headers.IndexOf(FilePathColumn);
                if (filePathIndex < 0) return; // Cannot match without FilePath column

                _availableColumns = headers.Where(h => h != FilePathColumn).ToList();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var fields = ParseCsvLine(line);
                    if (fields.Count <= filePathIndex) continue;

                    var filePath = fields[filePathIndex];
                    // Match on filename only
                    var filename = Path.GetFileName(filePath);
                    if (string.IsNullOrEmpty(filename)) continue;

                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < headers.Count && i < fields.Count; i++)
                    {
                        if (headers[i] != FilePathColumn)
                            row[headers[i]] = fields[i];
                    }

                    // Last row wins if duplicate filenames exist
                    _rowsByFilename[filename] = row;
                }

            }
            catch (Exception ex)
            {
                App.LoggingService?.LogError("CsvMetadataService", $"Failed to load {csvPath}", ex);
            }
        }

        private void LoadAcquisitionDetails(string folderPath)
        {
            var csvPath = Path.Combine(folderPath, AcquisitionDetailsFileName);
            if (!File.Exists(csvPath))
                return;

            try
            {
                using var reader = new StreamReader(csvPath);
                var headerLine = reader.ReadLine();
                if (headerLine == null) return;

                var headers = ParseCsvLine(headerLine);
                var filePathIndex = headers.IndexOf(FilePathColumn);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var fields = ParseCsvLine(line);

                    // If this file includes FilePath, merge values per filename.
                    if (filePathIndex >= 0 && fields.Count > filePathIndex)
                    {
                        var filePath = fields[filePathIndex];
                        var filename = Path.GetFileName(filePath);
                        if (!string.IsNullOrEmpty(filename))
                        {
                            if (!_rowsByFilename.TryGetValue(filename, out var row))
                            {
                                row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                _rowsByFilename[filename] = row;
                            }

                            for (int i = 0; i < headers.Count && i < fields.Count; i++)
                            {
                                if (!string.Equals(headers[i], FilePathColumn, StringComparison.OrdinalIgnoreCase))
                                    row[headers[i]] = fields[i];
                            }
                        }
                    }

                    // Also keep session-level fallback values (last non-empty wins).
                    for (int i = 0; i < headers.Count && i < fields.Count; i++)
                    {
                        if (string.Equals(headers[i], FilePathColumn, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrWhiteSpace(fields[i]))
                            _sessionValues[headers[i]] = fields[i];
                    }
                }

                // Merge discovered columns into available column list.
                var available = new HashSet<string>(_availableColumns, StringComparer.OrdinalIgnoreCase);
                foreach (var header in headers)
                {
                    if (!string.Equals(header, FilePathColumn, StringComparison.OrdinalIgnoreCase) && available.Add(header))
                        _availableColumns.Add(header);
                }
            }
            catch (Exception ex)
            {
                App.LoggingService?.LogError("CsvMetadataService", $"Failed to load {csvPath}", ex);
            }
        }

        /// <summary>
        /// Returns the values for the requested columns for a given filename.
        /// Returns an empty dictionary if the file has no matching row.
        /// Numeric values are formatted for display (3dp for &lt;1, 2dp for 1–9, integer for ≥10).
        /// </summary>
        public Dictionary<string, string> GetValues(string filename, IEnumerable<string> columns)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _rowsByFilename.TryGetValue(filename, out var row);

            foreach (var col in columns)
            {
                if (row != null && row.TryGetValue(col, out var rowVal) && !string.IsNullOrWhiteSpace(rowVal))
                    result[col] = FormatValue(rowVal);
                else if (_sessionValues.TryGetValue(col, out var sessionVal) && !string.IsNullOrWhiteSpace(sessionVal))
                    result[col] = FormatValue(sessionVal);
                else if (row != null && row.TryGetValue(col, out var val))
                    result[col] = FormatValue(val);
            }
            return result;
        }

        /// <summary>
        /// Formats a raw CSV string value for display.
        /// Numbers: &lt;1 → 3 decimal places; 1–9 → 2 decimal places; ≥10 → integer.
        /// Non-numeric strings are returned unchanged.
        /// </summary>
        private static string FormatValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            if (double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) return raw;

                double abs = Math.Abs(d);
                if (abs < 1.0)
                    return d.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                else if (abs < 10.0)
                    return d.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                else
                    return ((long)Math.Round(d)).ToString();
            }
            return raw;
        }

        /// <summary>True if any supported session metadata CSV was successfully loaded for the current folder.</summary>
        public bool IsLoaded => _rowsByFilename.Count > 0 || _sessionValues.Count > 0;

        /// <summary>
        /// Minimal CSV line parser that handles quoted fields containing commas.
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString().Trim());
            return fields;
        }
    }
}
