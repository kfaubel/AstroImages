using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AstroImages.Core;
using AstroImages.Utils;

namespace AstroImages.Wpf.Services
{
    public class KeywordExtractionService
    {
        public Dictionary<string, string> ExtractCustomKeywordsFromFilename(string filename, IEnumerable<string> keywords)
        {
            return FilenameParser.ExtractKeywordValues(filename, keywords);
        }

        public Dictionary<string, string> ExtractFitsKeywords(string filePath, IEnumerable<string> keywords, bool skipXisf = false)
        {
            var operationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new Dictionary<string, string>();

            if (!File.Exists(filePath) || !keywords.Any())
                return result;

            try
            {
                // Process FITS files
                if (FitsUtilities.IsFitsFile(filePath))
                {
                    // Use the optimized header-only reader instead of reading entire file
                    var fitsHeaders = FitsParser.ParseHeaderFromFile(filePath);

                    foreach (var keyword in keywords)
                    {
                        if (fitsHeaders.TryGetValue(keyword, out var value))
                        {
                            // Use the utility method for consistent formatting
                            result[keyword] = FitsUtilities.FormatHeaderValue(value);
                        }
                    }
                }
                // Process XISF files - they store FITS keywords with "FITS_" prefix
                else if (!skipXisf && XisfUtilities.IsXisfFile(filePath))
                {
                    try
                    {
                        // Debug: Log what keywords we're looking for
                        var keywordList = string.Join(", ", keywords);
                        System.Diagnostics.Debug.WriteLine($"Extracting XISF keywords from {System.IO.Path.GetFileName(filePath)}: {keywordList}");
                        
                        // Use the optimized method that only extracts requested keywords via regex
                        // This is much faster than ParseMetadataFromFile which parses all properties
                        var xisfKeywords = XisfParser.ParseSpecificFitsKeywords(filePath, keywords);
                        
                        System.Diagnostics.Debug.WriteLine($"Found {xisfKeywords.Count} keywords in {System.IO.Path.GetFileName(filePath)}");
                        
                        foreach (var kvp in xisfKeywords)
                        {
                            System.Diagnostics.Debug.WriteLine($"  {kvp.Key} = {kvp.Value}");
                            result[kvp.Key] = XisfUtilities.FormatPropertyValue(kvp.Value);
                        }
                    }
                    catch (Exception xisfEx)
                    {
                        // Log XISF-specific errors with more detail
                        App.LoggingService?.LogError("XISF Keyword Extraction", $"Failed to extract XISF keywords from {System.IO.Path.GetFileName(filePath)}: {xisfEx.Message}", xisfEx);
                        throw;
                    }
                }
                // Skip all other file formats (JPG, PNG, TIFF, etc.) for FITS keyword extraction
                // These files don't have FITS-style keywords, so return empty result
            }
            catch (Exception ex)
            {
                // Log unexpected parsing errors (corrupted files, I/O errors, etc.)
                App.LoggingService?.LogError("Keyword Extraction", $"Failed to extract keywords from {System.IO.Path.GetFileName(filePath)}", ex);
            }
            finally
            {
                operationStopwatch.Stop();
                if (operationStopwatch.ElapsedMilliseconds > 3000)
                {
                    App.LoggingService?.LogWarning("FITS Keyword Extraction", $"'{System.IO.Path.GetFileName(filePath)}' took {operationStopwatch.ElapsedMilliseconds}ms (>3s threshold)");
                }
            }

            return result;
        }



        public void PopulateKeywords(Models.FileItem fileItem, IEnumerable<string> customKeywords, IEnumerable<string> fitsKeywords, bool skipXisf = false)
        {
            PopulateKeywords(fileItem, customKeywords, fitsKeywords, skipXisf, calculateMedian: false);
        }

        public void PopulateKeywords(Models.FileItem fileItem, IEnumerable<string> customKeywords, IEnumerable<string> fitsKeywords, bool skipXisf, bool calculateMedian)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Debug: Log median calculation flag
            if (calculateMedian)
            {
                System.Diagnostics.Debug.WriteLine($"PopulateKeywords for {fileItem.Name}: calculateMedian=true");
            }
            
            // Extract custom keywords from filename
            if (customKeywords.Any())
            {
                var extractedCustom = ExtractCustomKeywordsFromFilename(fileItem.Name, customKeywords);
                fileItem.CustomKeywords = extractedCustom;
                
                // Debug log
                if (extractedCustom.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Extracted {extractedCustom.Count} custom keywords from {fileItem.Name}");
                }
            }

            // Extract FITS keywords from file headers
            if (fitsKeywords.Any())
            {
                var extractedFits = ExtractFitsKeywords(fileItem.Path, fitsKeywords, skipXisf);
                fileItem.FitsKeywords = extractedFits;
                
                // Debug log
                if (extractedFits.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Extracted {extractedFits.Count} FITS keywords from {fileItem.Name}");
                }
            }
            
            // Calculate median value for the image (only if requested)
            if (calculateMedian)
            {
                fileItem.Median = CalculateMedian(fileItem.Path);
            }
            
            stopwatch.Stop();
            
            // Log all files asynchronously to avoid blocking
            var ext = System.IO.Path.GetExtension(fileItem.Name).ToUpperInvariant();
            var elapsed = stopwatch.ElapsedMilliseconds;
            var customCount = fileItem.CustomKeywords?.Count ?? 0;
            var fitsCount = fileItem.FitsKeywords?.Count ?? 0;
            
            // Queue the log entry instead of writing synchronously
            System.Threading.Tasks.Task.Run(() => 
            {
                App.LoggingService?.LogInfo($"Loaded metadata: {fileItem.Name} ({ext}) in {elapsed}ms - {customCount} custom, {fitsCount} FITS keywords");
            });
        }

        /// <summary>
        /// Calculate the median pixel value for an image file (0.0-1.0 range).
        /// Public method for calculating median on demand.
        /// </summary>
        public double? CalculateMedianForFile(string filePath)
        {
            return CalculateMedian(filePath);
        }

        /// <summary>
        /// Calculate the median pixel value for an image file (0.0-1.0 range).
        /// Supports FITS, XISF, and standard image formats.
        /// </summary>
        private double? CalculateMedian(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    App.LoggingService?.LogWarning("Median Calculation", $"File not found: {System.IO.Path.GetFileName(filePath)}");
                    return null;
                }

                // Handle FITS files
                if (FitsUtilities.IsFitsFile(filePath))
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    if (!FitsUtilities.IsFitsData(bytes))
                    {
                        App.LoggingService?.LogWarning("Median Calculation", $"Invalid FITS data: {System.IO.Path.GetFileName(filePath)}");
                        return null;
                    }

                    var (width, height, pixels) = AstroImages.Core.FitsParser.ReadImage(bytes);
                    var median = CalculateMedianFromBytes(pixels);
                    System.Diagnostics.Debug.WriteLine($"Calculated median for FITS {System.IO.Path.GetFileName(filePath)}: {median:F3}");
                    return median;
                }
                // Handle XISF files
                else if (XisfUtilities.IsXisfFile(filePath))
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    var (width, height, pixels) = AstroImages.Utils.XisfParser.ReadImage(bytes);
                    var median = CalculateMedianFromBytes(pixels);
                    System.Diagnostics.Debug.WriteLine($"Calculated median for XISF {System.IO.Path.GetFileName(filePath)}: {median:F3}");
                    return median;
                }
                // Handle standard image formats (JPEG, PNG, etc.)
                else
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(filePath));
                    var median = CalculateMedianFromBitmap(bitmap);
                    System.Diagnostics.Debug.WriteLine($"Calculated median for image {System.IO.Path.GetFileName(filePath)}: {median:F3}");
                    return median;
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - median is optional
                App.LoggingService?.LogWarning("Median Calculation", $"Failed to calculate median for {System.IO.Path.GetFileName(filePath)}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Median calculation error for {System.IO.Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate median from byte array (0-255 range) normalized to 0.0-1.0
        /// </summary>
        private double CalculateMedianFromBytes(byte[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return 0.0;

            var sorted = pixels.OrderBy(p => p).ToArray();
            int middle = sorted.Length / 2;
            
            double median;
            if (sorted.Length % 2 == 0)
            {
                median = (sorted[middle - 1] + sorted[middle]) / 2.0;
            }
            else
            {
                median = sorted[middle];
            }
            
            // Normalize to 0.0-1.0 range
            return median / 255.0;
        }

        /// <summary>
        /// Calculate median from a BitmapSource (standard image)
        /// </summary>
        private double? CalculateMedianFromBitmap(System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            if (bitmap == null)
                return null;

            try
            {
                // Convert bitmap to a known format (Bgra32) for consistent pixel reading
                var convertedBitmap = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                    bitmap, 
                    System.Windows.Media.PixelFormats.Bgra32, 
                    null, 
                    0);

                int width = convertedBitmap.PixelWidth;
                int height = convertedBitmap.PixelHeight;
                int stride = width * 4; // 4 bytes per pixel (BGRA)
                byte[] pixels = new byte[stride * height];
                
                convertedBitmap.CopyPixels(pixels, stride, 0);
                
                // Convert BGRA to grayscale and calculate median
                var grayscalePixels = new List<byte>(width * height);
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte b = pixels[i];
                    byte g = pixels[i + 1];
                    byte r = pixels[i + 2];
                    // Standard grayscale conversion (ITU-R BT.601)
                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
                    grayscalePixels.Add(gray);
                }
                
                return CalculateMedianFromBytes(grayscalePixels.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculateMedianFromBitmap: {ex.Message}");
                return null;
            }
        }
    }
}