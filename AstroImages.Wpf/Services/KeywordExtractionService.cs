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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
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
    }
}