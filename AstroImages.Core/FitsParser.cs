using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AstroImages.Core
{
    /// <summary>
    /// Professional-grade FITS parser with enhanced capabilities and robust error handling.
    /// Provides comprehensive FITS file support including all standard data types, scaling parameters,
    /// and astronomical metadata extraction while maintaining full FITS standard compliance.
    /// </summary>
    public static class FitsParser
    {
        /// <summary>
        /// Parse FITS header from byte buffer and return header dictionary
        /// </summary>
        public static Dictionary<string, object> ParseHeader(byte[] buffer)
        {
            var (header, _) = ParseHeaderWithSize(buffer);
            return header;
        }

        /// <summary>
        /// Enhanced FITS header parser with comprehensive support for all FITS header types
        /// and robust error handling
        /// </summary>
        public static (Dictionary<string, object> header, int headerSize) ParseHeaderWithSize(byte[] buffer)
        {
            const int blockSize = 2880;  // FITS standard block size
            const int cardSize = 80;     // FITS standard card size
            
            var header = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            int totalHeaderSize = 0;
            bool foundEnd = false;
            
            try
            {
                // Process up to 100 blocks (288KB) to handle very large headers
                for (int blockIndex = 0; blockIndex < 100 && (blockIndex * blockSize) < buffer.Length; blockIndex++)
                {
                    int blockOffset = blockIndex * blockSize;
                    if (blockOffset + blockSize > buffer.Length) break;
                    
                    var blockBytes = new byte[blockSize];
                    Array.Copy(buffer, blockOffset, blockBytes, 0, blockSize);
                    var blockString = Encoding.ASCII.GetString(blockBytes);
                    
                    // Process each 80-character card in the block
                    for (int cardStart = 0; cardStart < blockSize; cardStart += cardSize)
                    {
                        if (cardStart + cardSize > blockSize) break;
                        
                        var card = blockString.Substring(cardStart, cardSize);
                        
                        // Check for END card
                        if (card.StartsWith("END") && (card.Length < 4 || card[3] == ' '))
                        {
                            totalHeaderSize = blockOffset + blockSize;
                            foundEnd = true;
                            break;
                        }
                        
                        // Parse the card
                        var parsedCard = ParseFitsCard(card);
                        if (parsedCard.HasValue)
                        {
                            var (keyword, value, comment) = parsedCard.Value;
                            
                            // Store the value, allowing extension headers to override primary
                            header[keyword] = value;
                            
                            // Also store comments for special keywords
                            if (!string.IsNullOrEmpty(comment) && 
                                (keyword == "COMMENT" || keyword == "HISTORY" || keyword == ""))
                            {
                                var commentKey = string.IsNullOrEmpty(keyword) ? "BLANK" : keyword;
                                if (header.ContainsKey($"{commentKey}_COMMENTS"))
                                {
                                    header[$"{commentKey}_COMMENTS"] += "\n" + comment;
                                }
                                else
                                {
                                    header[$"{commentKey}_COMMENTS"] = comment;
                                }
                            }
                        }
                    }
                    
                    if (foundEnd) break;
                }
                
                // If no END card found, estimate header size
                if (totalHeaderSize == 0)
                {
                    totalHeaderSize = blockSize; // Minimum one block
                }
                
                return (header, totalHeaderSize);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse FITS header: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enhanced FITS image reader with comprehensive data type support and proper scaling
        /// </summary>
        public static (int width, int height, byte[] pixels) ReadImage(byte[] buffer)
        {
            try
            {
                var (header, headerSize) = ParseHeaderWithSize(buffer);
                
                // Validate required header keywords
                if (!header.TryGetValue("NAXIS", out var naxisObj))
                    throw new InvalidOperationException("Missing NAXIS keyword in FITS header");
                
                int naxis = Convert.ToInt32(naxisObj);
                if (naxis < 2)
                    throw new InvalidOperationException($"FITS file has {naxis} dimensions, need at least 2 for image data");
                
                if (!header.TryGetValue("NAXIS1", out var wObj) || !header.TryGetValue("NAXIS2", out var hObj))
                    throw new InvalidOperationException("Missing NAXIS1/NAXIS2 in FITS header");
                
                int width = Convert.ToInt32(wObj);
                int height = Convert.ToInt32(hObj);
                
                if (width <= 0 || height <= 0)
                    throw new InvalidOperationException($"Invalid image dimensions: {width}x{height}");
                
                if (!header.TryGetValue("BITPIX", out var bitpixObj))
                    throw new InvalidOperationException("Missing BITPIX in FITS header");
                
                int bitpix = Convert.ToInt32(bitpixObj);
                
                // Get scaling parameters (FITS standard)
                double bzero = header.TryGetValue("BZERO", out var bzeroObj) ? Convert.ToDouble(bzeroObj) : 0.0;
                double bscale = header.TryGetValue("BSCALE", out var bscaleObj) ? Convert.ToDouble(bscaleObj) : 1.0;
                
                // Calculate expected data size
                int bytesPerPixel = Math.Abs(bitpix) / 8;
                long expectedDataSize = (long)width * height * bytesPerPixel;
                
                if (buffer.Length < headerSize + expectedDataSize)
                    throw new ArgumentOutOfRangeException(
                        $"FITS buffer too small: expected {headerSize + expectedDataSize} bytes, got {buffer.Length}");
                
                // Convert image data to byte array
                var pixels = new byte[width * height];
                ConvertImageData(buffer, headerSize, pixels, width, height, bitpix, bzero, bscale);
                
                return (width, height, pixels);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read FITS image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse a single FITS header card (80 characters)
        /// </summary>
        private static (string keyword, object value, string comment)? ParseFitsCard(string card)
        {
            if (string.IsNullOrWhiteSpace(card) || card.Length != 80)
                return null;
            
            // Extract keyword (first 8 characters, trimmed)
            string keyword = card.Substring(0, 8).Trim();
            
            if (string.IsNullOrEmpty(keyword))
                return null;
            
            // Handle special cases
            if (keyword == "COMMENT" || keyword == "HISTORY")
            {
                string comment = card.Substring(8).Trim();
                return (keyword, comment, comment);
            }
            
            // Check for value indicator (column 8 should be '=')
            if (card.Length > 8 && card[8] == '=')
            {
                // Extract value and comment
                string valueAndComment = card.Substring(9).Trim();
                string valueStr = valueAndComment;
                string comment = "";
                
                // Find comment separator
                int commentStart = FindCommentStart(valueAndComment);
                if (commentStart >= 0)
                {
                    valueStr = valueAndComment.Substring(0, commentStart).Trim();
                    comment = valueAndComment.Substring(commentStart + 1).Trim();
                }
                
                // Parse the value
                object value = ParseFitsValue(valueStr);
                return (keyword, value, comment);
            }
            else
            {
                // No value, treat as comment
                string comment = card.Substring(8).Trim();
                return (keyword, "", comment);
            }
        }

        /// <summary>
        /// Find the start of a comment in a FITS value string
        /// </summary>
        private static int FindCommentStart(string valueStr)
        {
            bool inQuotes = false;
            for (int i = 0; i < valueStr.Length; i++)
            {
                if (valueStr[i] == '\'')
                {
                    inQuotes = !inQuotes;
                }
                else if (valueStr[i] == '/' && !inQuotes)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parse a FITS value string to appropriate .NET type
        /// </summary>
        private static object ParseFitsValue(string valueStr)
        {
            if (string.IsNullOrWhiteSpace(valueStr))
                return "";
            
            valueStr = valueStr.Trim();
            
            // Handle quoted strings
            if (valueStr.StartsWith("'"))
            {
                if (valueStr.EndsWith("'") && valueStr.Length >= 2)
                {
                    return valueStr.Substring(1, valueStr.Length - 2);
                }
                else
                {
                    // Malformed quoted string, return as-is
                    return valueStr;
                }
            }
            
            // Handle boolean values
            if (valueStr.Equals("T", StringComparison.OrdinalIgnoreCase))
                return true;
            if (valueStr.Equals("F", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Handle numeric values
            if (long.TryParse(valueStr, out long longValue))
            {
                // Return as int if it fits, otherwise as long
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                    return (int)longValue;
                else
                    return longValue;
            }
            
            if (double.TryParse(valueStr, out double doubleValue))
            {
                return doubleValue;
            }
            
            // Return as string if all else fails
            return valueStr;
        }

        /// <summary>
        /// Convert FITS image data to byte array with comprehensive type support
        /// </summary>
        private static void ConvertImageData(byte[] buffer, int dataOffset, byte[] pixels, 
            int width, int height, int bitpix, double bzero, double bscale)
        {
            switch (bitpix)
            {
                case 8:   // Unsigned 8-bit integer
                    ConvertUInt8Data(buffer, dataOffset, pixels, bzero, bscale);
                    break;
                case 16:  // Signed 16-bit integer
                    ConvertInt16Data(buffer, dataOffset, pixels, width, height, bzero, bscale);
                    break;
                case 32:  // Signed 32-bit integer
                    ConvertInt32Data(buffer, dataOffset, pixels, width, height, bzero, bscale);
                    break;
                case -32: // 32-bit floating point
                    ConvertFloat32Data(buffer, dataOffset, pixels, width, height, bzero, bscale);
                    break;
                case -64: // 64-bit floating point
                    ConvertFloat64Data(buffer, dataOffset, pixels, width, height, bzero, bscale);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported BITPIX value: {bitpix}");
            }
        }

        private static void ConvertUInt8Data(byte[] buffer, int offset, byte[] pixels, double bzero, double bscale)
        {
            for (int i = 0; i < pixels.Length && offset + i < buffer.Length; i++)
            {
                double val = buffer[offset + i] * bscale + bzero;
                pixels[i] = (byte)Math.Clamp(val, 0, 255);
            }
        }

        private static void ConvertInt16Data(byte[] buffer, int offset, byte[] pixels, int width, int height, 
            double bzero, double bscale)
        {
            int pixelCount = width * height;
            var values = new double[pixelCount];
            double min = double.MaxValue, max = double.MinValue;
            
            // Read and scale all values, find min/max
            for (int i = 0; i < pixelCount && offset + i * 2 + 1 < buffer.Length; i++)
            {
                // FITS uses big-endian byte order
                short rawValue = (short)((buffer[offset + i * 2] << 8) | buffer[offset + i * 2 + 1]);
                double scaledValue = rawValue * bscale + bzero;
                values[i] = scaledValue;
                
                if (scaledValue < min) min = scaledValue;
                if (scaledValue > max) max = scaledValue;
            }
            
            // Convert to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                // All values are the same
                byte constValue = (byte)Math.Clamp(values[0], 0, 255);
                Array.Fill(pixels, constValue);
            }
        }

        private static void ConvertInt32Data(byte[] buffer, int offset, byte[] pixels, int width, int height, 
            double bzero, double bscale)
        {
            int pixelCount = width * height;
            var values = new double[pixelCount];
            double min = double.MaxValue, max = double.MinValue;
            
            // Read and scale all values, find min/max
            for (int i = 0; i < pixelCount && offset + i * 4 + 3 < buffer.Length; i++)
            {
                // FITS uses big-endian byte order
                int rawValue = (buffer[offset + i * 4] << 24) | 
                              (buffer[offset + i * 4 + 1] << 16) | 
                              (buffer[offset + i * 4 + 2] << 8) | 
                               buffer[offset + i * 4 + 3];
                
                double scaledValue = rawValue * bscale + bzero;
                values[i] = scaledValue;
                
                if (scaledValue < min) min = scaledValue;
                if (scaledValue > max) max = scaledValue;
            }
            
            // Convert to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(values[0], 0, 255);
                Array.Fill(pixels, constValue);
            }
        }

        private static void ConvertFloat32Data(byte[] buffer, int offset, byte[] pixels, int width, int height, 
            double bzero, double bscale)
        {
            int pixelCount = width * height;
            var values = new double[pixelCount];
            double min = double.MaxValue, max = double.MinValue;
            
            // Read and scale all values, find min/max
            for (int i = 0; i < pixelCount && offset + i * 4 + 3 < buffer.Length; i++)
            {
                // Convert big-endian bytes to float
                byte[] floatBytes = new byte[4];
                floatBytes[3] = buffer[offset + i * 4];
                floatBytes[2] = buffer[offset + i * 4 + 1];
                floatBytes[1] = buffer[offset + i * 4 + 2];
                floatBytes[0] = buffer[offset + i * 4 + 3];
                
                float rawValue = BitConverter.ToSingle(floatBytes, 0);
                
                if (float.IsFinite(rawValue))
                {
                    double scaledValue = rawValue * bscale + bzero;
                    values[i] = scaledValue;
                    
                    if (scaledValue < min) min = scaledValue;
                    if (scaledValue > max) max = scaledValue;
                }
                else
                {
                    values[i] = 0; // Handle NaN/Infinity
                }
            }
            
            // Convert to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(min, 0, 255);
                Array.Fill(pixels, constValue);
            }
        }

        private static void ConvertFloat64Data(byte[] buffer, int offset, byte[] pixels, int width, int height, 
            double bzero, double bscale)
        {
            int pixelCount = width * height;
            var values = new double[pixelCount];
            double min = double.MaxValue, max = double.MinValue;
            
            // Read and scale all values, find min/max
            for (int i = 0; i < pixelCount && offset + i * 8 + 7 < buffer.Length; i++)
            {
                // Convert big-endian bytes to double
                byte[] doubleBytes = new byte[8];
                for (int j = 0; j < 8; j++)
                {
                    doubleBytes[7 - j] = buffer[offset + i * 8 + j];
                }
                
                double rawValue = BitConverter.ToDouble(doubleBytes, 0);
                
                if (double.IsFinite(rawValue))
                {
                    double scaledValue = rawValue * bscale + bzero;
                    values[i] = scaledValue;
                    
                    if (scaledValue < min) min = scaledValue;
                    if (scaledValue > max) max = scaledValue;
                }
                else
                {
                    values[i] = 0; // Handle NaN/Infinity
                }
            }
            
            // Convert to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(min, 0, 255);
                Array.Fill(pixels, constValue);
            }
        }
    }
}
