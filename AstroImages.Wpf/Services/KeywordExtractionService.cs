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

        public Dictionary<string, string> ExtractFitsKeywords(string filePath, IEnumerable<string> keywords)
        {
            var result = new Dictionary<string, string>();

            if (!File.Exists(filePath) || !keywords.Any())
                return result;

            try
            {
                // Only process FITS files for FITS keyword extraction
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
                // Skip all other file formats (XISF, JPG, PNG, TIFF) for FITS keyword extraction
                // These files don't have FITS-style keywords, so return empty result
            }
            catch (Exception)
            {
                // If parsing fails, return empty dictionary
            }

            return result;
        }



        public void PopulateKeywords(Models.FileItem fileItem, IEnumerable<string> customKeywords, IEnumerable<string> fitsKeywords)
        {
            // Extract custom keywords from filename
            if (customKeywords.Any())
            {
                fileItem.CustomKeywords = ExtractCustomKeywordsFromFilename(fileItem.Name, customKeywords);
            }

            // Extract FITS keywords from file headers
            if (fitsKeywords.Any())
            {
                fileItem.FitsKeywords = ExtractFitsKeywords(fileItem.Path, fitsKeywords);
            }
        }
    }
}