using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ApexAstro.Wpf.Models;

namespace ApexAstro.Wpf.Services
{
    /// <summary>
    /// Caches computed median and mean pixel values in AstroData.csv within each image folder.
    /// On folder open, cached values are used when the file's last-write timestamp matches what
    /// was recorded; if the file is newer the stats are recomputed.  Entries for files that no
    /// longer exist are automatically discarded when the cache is saved.
    ///
    /// CSV format (invariant-culture, no quoting needed for these fields):
    ///   FileName,DateTimeModified,Median,Mean
    /// DateTimeModified is stored in ISO 8601 round-trip format (UTC).
    /// </summary>
    public class AstroDataCacheService
    {
        private const string CacheFileName = "AstroData.csv";

        // In-memory cache: filename (case-insensitive) → (lastModifiedUtc, median, mean)
        private Dictionary<string, (DateTime lastModified, double? median, double? mean)> _cache
            = new(StringComparer.OrdinalIgnoreCase);

        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Loads AstroData.csv from <paramref name="folderPath"/> into memory.
        /// Any previous cache data is discarded.
        /// </summary>
        public void Load(string folderPath)
        {
            _cache = new Dictionary<string, (DateTime, double?, double?)>(StringComparer.OrdinalIgnoreCase);

            var cachePath = Path.Combine(folderPath, CacheFileName);
            if (!File.Exists(cachePath))
                return;

            try
            {
                using var reader = new StreamReader(cachePath);

                // Skip header line
                var header = reader.ReadLine();
                if (header == null) return;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;

                    var fileName = parts[0].Trim();
                    if (string.IsNullOrEmpty(fileName)) continue;

                    if (!DateTime.TryParseExact(parts[1].Trim(), "O",
                            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                            out var lastModified))
                        continue;

                    double? median = null;
                    double? mean = null;

                    if (double.TryParse(parts[2].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var m))
                        median = m;

                    if (double.TryParse(parts[3].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var mn))
                        mean = mn;

                    _cache[fileName] = (lastModified, median, mean);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AstroDataCacheService.Load failed: {ex.Message}");
                _cache.Clear(); // treat corrupt cache as empty — everything will be recomputed
            }
        }

        /// <summary>
        /// Returns cached median and mean values if the entry exists and
        /// the file's current last-write timestamp matches the cached one.
        /// Returns <c>false</c> if the file is absent from the cache or has been modified.
        /// Thread-safe for concurrent reads (no write occurs here).
        /// </summary>
        public bool TryGetCached(string fileName, DateTime fileLastWriteUtc,
            out double? median, out double? mean)
        {
            median = null;
            mean = null;

            if (!_cache.TryGetValue(fileName, out var entry))
                return false;

            // Allow 2-second tolerance to handle FAT/NTFS precision differences
            if (Math.Abs((entry.lastModified - fileLastWriteUtc).TotalSeconds) > 2.0)
                return false;

            median = entry.median;
            mean = entry.mean;
            return true;
        }

        /// <summary>
        /// Writes the cache to AstroData.csv in <paramref name="folderPath"/>.
        /// Only files present in <paramref name="fileItems"/> that have at least one
        /// computed stat value are written — entries for missing/removed files are
        /// naturally excluded (purged).
        /// </summary>
        public void Save(string folderPath, IEnumerable<FileItem> fileItems)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            try
            {
                var cachePath = Path.Combine(folderPath, CacheFileName);

                using var writer = new StreamWriter(cachePath, append: false);
                writer.WriteLine("FileName,DateTimeModified,Median,Mean");

                foreach (var fileItem in fileItems)
                {
                    if (!fileItem.Median.HasValue && !fileItem.Mean.HasValue)
                        continue; // No stats computed for this file; skip

                    // Obtain the file's last-write time (used as the cache key for staleness checks)
                    DateTime lastWrite;
                    try
                    {
                        lastWrite = File.GetLastWriteTimeUtc(fileItem.Path);
                    }
                    catch
                    {
                        continue; // File inaccessible; skip
                    }

                    var medianStr = fileItem.Median.HasValue
                        ? fileItem.Median.Value.ToString("G8", CultureInfo.InvariantCulture)
                        : string.Empty;

                    var meanStr = fileItem.Mean.HasValue
                        ? fileItem.Mean.Value.ToString("G8", CultureInfo.InvariantCulture)
                        : string.Empty;

                    writer.WriteLine($"{fileItem.Name},{lastWrite:O},{medianStr},{meanStr}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AstroDataCacheService.Save failed: {ex.Message}");
            }
        }
    }
}
