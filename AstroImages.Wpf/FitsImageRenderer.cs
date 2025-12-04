using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using AstroImages.Core;
using AstroImages.Utils;

namespace AstroImages.Wpf
{
    /// <summary>
    /// Cached XISF header information to avoid redundant parsing
    /// </summary>
    public class XisfHeaderCache
    {
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Channels { get; set; }
        public string SampleFormat { get; set; } = "";
        public long ImagePosition { get; set; }
        public long ImageSize { get; set; }
        public byte[]? FileBytes { get; set; } // Cache the file bytes for reuse
    }

    public static class FitsImageRenderer
    {
        private static readonly ConcurrentDictionary<string, XisfHeaderCache> _xisfCache = new();
        
        public static BitmapSource? RenderFitsFile(string filePath, bool autoStretch = true)
        {
            try
            {
                // Check if it's an XISF file first
                if (XisfUtilities.IsXisfFile(filePath))
                {
                    return RenderXisfFile(filePath, autoStretch);
                }
                
                // Check if it's a FITS file
                if (FitsUtilities.IsFitsFile(filePath))
                {
                    return RenderFitsFileInternal(filePath, autoStretch);
                }
                
                // Try to render as standard image format (stretching doesn't apply to standard images)
                return RenderStandardImage(filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to render image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Render standard image formats (JPG, PNG, BMP, TIFF, etc.)
        /// </summary>
        private static BitmapSource? RenderStandardImage(string filePath)
        {
            try
            {
                // Try multiple approaches for loading standard images
                BitmapSource bitmap;
                
                try
                {
                    // First approach: Use BitmapImage with file URI
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load immediately and release file handle
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // Make it thread-safe and immutable
                    bitmap = bitmapImage;
                }
                catch (Exception)
                {
                    // Second approach: Load from stream
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        bitmap = bitmapImage;
                    }
                }
                
                if (bitmap != null)
                {
                    // Check if the image has transparency or unusual format that might cause display issues
                    if (bitmap.Format == PixelFormats.Pbgra32 || bitmap.Format == PixelFormats.Prgba64)
                    {
                        // Image has transparency/alpha channel
                    }
                    
                    // Sample a few pixels to see if there's actual image data (before format conversion)
                    try
                    {
                        if (bitmap.Format == PixelFormats.Bgr32 || bitmap.Format == PixelFormats.Bgra32)
                        {
                            // Use a simple approach - just sample the first few pixels from first row
                            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
                            int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
                            byte[] pixels = new byte[stride]; // Just sample first row
                            bitmap.CopyPixels(new Int32Rect(0, 0, bitmap.PixelWidth, 1), pixels, stride, 0);
                            
                            // Check first few pixels for non-zero values
                            int pixelsToCheck = Math.Min(10, bitmap.PixelWidth);
                            for (int i = 0; i < pixelsToCheck * bytesPerPixel; i += bytesPerPixel)
                            {
                                if (pixels[i] > 5 || pixels[i + 1] > 5 || pixels[i + 2] > 5) // B, G, R channels
                                {
                                    // Found non-zero pixel data
                                    break;
                                }
                            }
                            // Pixel data validation completed
                        }
                    }
                    catch (Exception)
                    {
                        // Error sampling pixel data - continue with conversion
                    }
                    
                    // Convert problematic formats to ensure proper display
                    if (bitmap.Format == PixelFormats.Bgr32)
                    {
                        // Create a new WriteableBitmap with the correct format
                        var writeableBitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Bgra32, null);
                        
                        // Copy pixel data from original to new bitmap
                        int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
                        byte[] pixels = new byte[stride * bitmap.PixelHeight];
                        bitmap.CopyPixels(pixels, stride, 0);
                        
                        // Convert BGR32 to BGRA32 by adding alpha channel - OPTIMIZED VERSION
                        // Both BGR32 and BGRA32 use 4 bytes per pixel, so stride is the same
                        byte[] newPixels = new byte[pixels.Length]; // Same size since both are 4 bytes per pixel
                        
                        // Fast bulk copy of all RGB data in one operation
                        Buffer.BlockCopy(pixels, 0, newPixels, 0, pixels.Length);
                        
                        // Set alpha channel to 255 for all pixels using vectorized iteration
                        for (int i = 3; i < newPixels.Length; i += 4)
                        {
                            newPixels[i] = 255; // Alpha = full opacity
                        }
                        
                        // Write the pixel data to the WriteableBitmap
                        writeableBitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), newPixels, stride, 0);
                        writeableBitmap.Freeze();
                        bitmap = writeableBitmap;
                    }
                    
                    return bitmap;
                }
                
                return null;
            }
            catch (Exception)
            {
                // Failed to load standard image
                return null;
            }
        }

        private static BitmapSource? RenderFitsFileInternal(string filePath, bool autoStretch = true)
        {
            try
            {

                var bytes = File.ReadAllBytes(filePath);
                
                // Validate the FITS data structure
                if (!FitsUtilities.IsFitsData(bytes))
                {
                    throw new InvalidOperationException("File does not contain valid FITS data");
                }

                var (width, height, pixels) = FitsParser.ReadImage(bytes);
                
                // Validate image dimensions
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException($"Invalid image dimensions: {width}x{height}");
                }

                // Calculate basic statistics for debugging
                var stats = FitsUtilities.CalculateImageStatistics(pixels);
                
                // Apply stretching algorithm if enabled, otherwise use raw pixel values
                var scaledPixels = autoStretch ? ApplyAutoStretch(pixels, width, height) : pixels;

                // Check for potential issues and provide informative messages
                if (Math.Abs(stats["Max"] - stats["Min"]) < 0.001)
                {
                    System.Windows.MessageBox.Show($"FITS image has uniform pixel values (Min={stats["Min"]:F1}, Max={stats["Max"]:F1}). Image may appear blank.", 
                        "FITS Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else if (stats["Max"] < 1)
                {
                    System.Windows.MessageBox.Show($"FITS image has very low pixel values (Max={stats["Max"]:F1}). This may be a bias frame or calibration image.", 
                        "FITS Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }

                // Create WPF bitmap with proper stride calculation
                // For Gray8 format, each pixel is 1 byte, so stride = width in bytes
                // But WPF requires stride to be aligned to 4-byte boundary
                int stride = width; // 1 byte per pixel for Gray8
                int alignedStride = ((stride + 3) / 4) * 4; // Round up to nearest multiple of 4

                // Create properly padded pixel data
                byte[] paddedPixels;
                if (alignedStride == width)
                {
                    // No padding needed
                    paddedPixels = scaledPixels;
                }
                else
                {
                    // Need to pad each row to meet alignment requirements
                    paddedPixels = new byte[alignedStride * height];
                    for (int y = 0; y < height; y++)
                    {
                        Array.Copy(scaledPixels, y * width, paddedPixels, y * alignedStride, width);
                        // The extra bytes in each row remain 0 (padding)
                    }
                    stride = alignedStride; // Use the aligned stride
                }

                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96, // DPI
                    PixelFormats.Gray8,
                    null, // palette
                    paddedPixels,
                    stride
                );

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error rendering FITS file: {ex.Message}", "FITS Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        private static byte[] ApplyAutoStretch(byte[] originalPixels, int width, int height)
        {
            // Convert back to larger data type for better processing
            var floatPixels = new float[originalPixels.Length];
            for (int i = 0; i < originalPixels.Length; i++)
            {
                floatPixels[i] = originalPixels[i];
            }
            
            // Calculate histogram for better stretching
            var histogram = new int[256];
            foreach (var pixel in originalPixels)
            {
                histogram[pixel]++;
            }
            
            // Find 1% and 99% percentiles for better contrast
            int totalPixels = width * height;
            int lowCount = (int)(totalPixels * 0.01);
            int highCount = (int)(totalPixels * 0.99);
            
            int lowValue = 0, highValue = 255;
            int count = 0;
            
            // Find low percentile
            for (int i = 0; i < 256; i++)
            {
                count += histogram[i];
                if (count >= lowCount)
                {
                    lowValue = i;
                    break;
                }
            }
            
            // Find high percentile
            count = 0;
            for (int i = 255; i >= 0; i--)
            {
                count += histogram[i];
                if (count >= (totalPixels - highCount))
                {
                    highValue = i;
                    break;
                }
            }
            
            // Apply stretch
            var stretchedPixels = new byte[originalPixels.Length];
            float range = highValue - lowValue;
            if (range == 0) range = 1;
            
            for (int i = 0; i < originalPixels.Length; i++)
            {
                float normalized = (originalPixels[i] - lowValue) / range;
                normalized = Math.Max(0, Math.Min(1, normalized)); // Clamp to 0-1
                
                // Apply gamma correction for better visibility
                normalized = (float)Math.Pow(normalized, 0.8);
                
                // Scale to 0-255 range (normal representation: bright stars = white, dark sky = black)
                stretchedPixels[i] = (byte)(normalized * 255);
            }
            
            return stretchedPixels;
        }

        public static System.Collections.Generic.Dictionary<string, object> GetFitsHeaders(string filePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                return FitsParser.ParseHeader(bytes);
            }
            catch (Exception)
            {
                return new System.Collections.Generic.Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Render XISF file to BitmapSource with optimized performance
        /// </summary>
        private static BitmapSource? RenderXisfFile(string filePath, bool autoStretch = true)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine($"[FitsImageRenderer] Starting optimized XISF render for {System.IO.Path.GetFileName(filePath)}");
                
                // Get cached header information
                var cacheStart = stopwatch.ElapsedMilliseconds;
                var cache = GetOrCreateXisfCache(filePath);
                Console.WriteLine($"[FitsImageRenderer] Cache/header processing took {stopwatch.ElapsedMilliseconds - cacheStart}ms");
                
                // Load file bytes if not already loaded
                if (cache.FileBytes == null)
                {
                    var loadStart = stopwatch.ElapsedMilliseconds;
                    cache.FileBytes = File.ReadAllBytes(filePath);
                    Console.WriteLine($"[FitsImageRenderer] File load took {stopwatch.ElapsedMilliseconds - loadStart}ms ({cache.FileBytes.Length:N0} bytes)");
                }
                
                Console.WriteLine($"[FitsImageRenderer] Detected {cache.Channels} channel(s) in XISF file {System.IO.Path.GetFileName(filePath)} ({cache.Width}x{cache.Height}, {cache.SampleFormat})");
                
                if (cache.Channels >= 3)
                {
                    // Multi-channel RGB file - use optimized RGB rendering
                    Console.WriteLine($"[FitsImageRenderer] Using optimized RGB rendering for multi-channel XISF file");
                    var rgbRenderStart = stopwatch.ElapsedMilliseconds;
                    try
                    {
                        var rgbPixels = RenderXisfRgbOptimized(cache, autoStretch);
                        Console.WriteLine($"[FitsImageRenderer] Optimized RGB processing took {stopwatch.ElapsedMilliseconds - rgbRenderStart}ms");

                        if (rgbPixels != null && rgbPixels.Length > 0)
                        {
                            var bitmapCreateStart = stopwatch.ElapsedMilliseconds;
                            // Create RGB bitmap
                            var rgbBitmap = new WriteableBitmap(cache.Width, cache.Height, 96, 96, PixelFormats.Rgb24, null);
                            rgbBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, cache.Width, cache.Height), rgbPixels, cache.Width * 3, 0);
                            rgbBitmap.Freeze();
                            Console.WriteLine($"[FitsImageRenderer] RGB bitmap creation took {stopwatch.ElapsedMilliseconds - bitmapCreateStart}ms");
                            Console.WriteLine($"[FitsImageRenderer] Total optimized RGB XISF render time: {stopwatch.ElapsedMilliseconds}ms for ({cache.Width}x{cache.Height})");

                            return rgbBitmap;
                        }
                    }
                    catch (Exception rgbEx)
                    {
                        Console.WriteLine($"[FitsImageRenderer] Optimized RGB rendering failed after {stopwatch.ElapsedMilliseconds - rgbRenderStart}ms: {rgbEx.Message}, falling back to grayscale");
                        // Fall through to grayscale
                    }
                }
                
                // Single channel or fallback - use optimized grayscale rendering
                Console.WriteLine($"[FitsImageRenderer] Using optimized grayscale rendering for XISF file");
                var grayscaleStart = stopwatch.ElapsedMilliseconds;
                var pixels = RenderXisfGrayscaleOptimized(cache, autoStretch);
                Console.WriteLine($"[FitsImageRenderer] Optimized grayscale processing took {stopwatch.ElapsedMilliseconds - grayscaleStart}ms");

                if (pixels == null || pixels.Length == 0)
                {
                    throw new InvalidOperationException("Invalid XISF image data");
                }

                // Apply additional auto-stretch if enabled (same as FITS files)
                if (autoStretch)
                {
                    var stretchStart = stopwatch.ElapsedMilliseconds;
                    pixels = ApplyAutoStretch(pixels, cache.Width, cache.Height);
                    Console.WriteLine($"[FitsImageRenderer] Auto-stretch took {stopwatch.ElapsedMilliseconds - stretchStart}ms");
                }

                var grayscaleBitmapStart = stopwatch.ElapsedMilliseconds;
                // Create grayscale bitmap
                var bitmap = new WriteableBitmap(cache.Width, cache.Height, 96, 96, PixelFormats.Gray8, null);
                bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, cache.Width, cache.Height), pixels, cache.Width, 0);
                bitmap.Freeze();
                Console.WriteLine($"[FitsImageRenderer] Grayscale bitmap creation took {stopwatch.ElapsedMilliseconds - grayscaleBitmapStart}ms");
                Console.WriteLine($"[FitsImageRenderer] Total optimized grayscale XISF render time: {stopwatch.ElapsedMilliseconds}ms");

                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FitsImageRenderer] Optimized XISF render failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                throw new InvalidOperationException($"Failed to render XISF image: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Get or create cached XISF header information
        /// </summary>
        private static XisfHeaderCache GetOrCreateXisfCache(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var cacheKey = filePath.ToLowerInvariant();
            
            // Check if we have a valid cache entry
            if (_xisfCache.TryGetValue(cacheKey, out var cachedInfo))
            {
                // Validate cache freshness
                if (cachedInfo.FileSize == fileInfo.Length && 
                    cachedInfo.LastModified.Equals(fileInfo.LastWriteTime))
                {
                    Console.WriteLine($"[FitsImageRenderer] Using cached XISF header for {Path.GetFileName(filePath)}");
                    return cachedInfo;
                }
                
                // Cache is stale, remove it
                _xisfCache.TryRemove(cacheKey, out _);
            }
            
            // Create new cache entry
            Console.WriteLine($"[FitsImageRenderer] Parsing XISF header for {Path.GetFileName(filePath)}");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var newCache = new XisfHeaderCache
            {
                FilePath = filePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime
            };
            
            try
            {
                // Parse header from file stream (don't load entire file yet)
                var headerParseStart = stopwatch.ElapsedMilliseconds;
                var headerInfo = ParseXisfHeaderFromFile(filePath);
                Console.WriteLine($"[FitsImageRenderer] Header parsing took {stopwatch.ElapsedMilliseconds - headerParseStart}ms");
                
                newCache.Width = headerInfo.Width;
                newCache.Height = headerInfo.Height;
                newCache.Channels = headerInfo.Channels;
                newCache.SampleFormat = headerInfo.SampleFormat;
                newCache.ImagePosition = headerInfo.ImagePosition;
                newCache.ImageSize = headerInfo.ImageSize;
                
                // Don't load file bytes yet - they'll be loaded on-demand when rendering
                newCache.FileBytes = null;
                
                // Cache the entry
                _xisfCache.TryAdd(cacheKey, newCache);
                Console.WriteLine($"[FitsImageRenderer] Total cache creation took {stopwatch.ElapsedMilliseconds}ms");
                
                return newCache;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FitsImageRenderer] Failed to cache XISF header: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Optimized XISF header parsing that extracts only essential information
        /// </summary>
        private static (int Width, int Height, int Channels, string SampleFormat, long ImagePosition, long ImageSize) ParseXisfHeaderOptimized(byte[] buffer)
        {
            if (buffer.Length < 16)
                throw new InvalidOperationException("Invalid XISF file: too small");
                
            // Validate signature
            var signature = System.Text.Encoding.ASCII.GetString(buffer, 0, 8);
            if (signature != "XISF0100")
                throw new InvalidOperationException("Invalid XISF signature");
                
            // Read header length (little-endian)
            int headerLength = BitConverter.ToInt32(buffer, 8);
            if (headerLength <= 0 || headerLength > buffer.Length - 16)
                throw new InvalidOperationException($"Invalid header length: {headerLength}");
                
            // Read header as UTF-8
            var headerXml = System.Text.Encoding.UTF8.GetString(buffer, 16, headerLength);
            
            return ParseXisfHeaderXml(headerXml);
        }
        
        /// <summary>
        /// Parse XISF header directly from file (for optimized caching without loading entire file)
        /// </summary>
        private static (int Width, int Height, int Channels, string SampleFormat, long ImagePosition, long ImageSize) ParseXisfHeaderFromFile(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read and validate signature
                byte[] signatureBuffer = new byte[8];
                stream.Read(signatureBuffer, 0, 8);
                var signature = System.Text.Encoding.ASCII.GetString(signatureBuffer);
                if (signature != "XISF0100")
                    throw new InvalidOperationException("Invalid XISF signature");
                
                // Read header length
                byte[] lengthBuffer = new byte[4];
                stream.Read(lengthBuffer, 0, 4);
                int headerLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                if (headerLength <= 0 || headerLength > 100 * 1024 * 1024) // Sanity check: max 100MB header
                    throw new InvalidOperationException($"Invalid header length: {headerLength}");
                
                // Skip reserved field (4 bytes)
                stream.Seek(4, SeekOrigin.Current);
                
                // Read header XML
                byte[] headerBuffer = new byte[headerLength];
                stream.Read(headerBuffer, 0, headerLength);
                var headerXml = System.Text.Encoding.UTF8.GetString(headerBuffer);
                
                return ParseXisfHeaderXml(headerXml);
            }
        }
        
        /// <summary>
        /// Parse XISF header XML to extract image metadata
        /// </summary>
        private static (int Width, int Height, int Channels, string SampleFormat, long ImagePosition, long ImageSize) ParseXisfHeaderXml(string headerXml)
        {
            // Fast XML parsing - look for essential attributes only
            int width = 0, height = 0, channels = 1;
            string sampleFormat = "UInt16";
            long imagePosition = 0, imageSize = 0;
            
            // Find first Image element geometry attribute
            var geometryMatch = System.Text.RegularExpressions.Regex.Match(headerXml, @"<Image[^>]*geometry\s*=\s*[""']([^""']+)[""']");
            if (geometryMatch.Success)
            {
                var geometryParts = geometryMatch.Groups[1].Value.Split(':');
                if (geometryParts.Length >= 3)
                {
                    int.TryParse(geometryParts[0], out width);
                    int.TryParse(geometryParts[1], out height);
                    int.TryParse(geometryParts[2], out channels);
                }
            }
            
            // Find sampleFormat attribute
            var formatMatch = System.Text.RegularExpressions.Regex.Match(headerXml, @"<Image[^>]*sampleFormat\s*=\s*[""']([^""']+)[""']");
            if (formatMatch.Success)
            {
                sampleFormat = formatMatch.Groups[1].Value;
            }
            
            // Find location attribute
            var locationMatch = System.Text.RegularExpressions.Regex.Match(headerXml, @"<Image[^>]*location\s*=\s*[""']attachment:(\d+):(\d+)[""']");
            if (locationMatch.Success)
            {
                long.TryParse(locationMatch.Groups[1].Value, out imagePosition);
                long.TryParse(locationMatch.Groups[2].Value, out imageSize);
            }
            
            return (width, height, channels, sampleFormat, imagePosition, imageSize);
        }
        
        /// <summary>
        /// Get the number of channels using cached information
        /// </summary>
        private static int GetXisfChannelCount(string filePath)
        {
            try
            {
                var cache = GetOrCreateXisfCache(filePath);
                return cache.Channels;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetXisfChannelCount] Exception: {ex.Message}");
                return 1; // Default to monochrome on error
            }
        }
        
        /// <summary>
        /// Legacy method for backward compatibility - avoid using this
        /// </summary>
        private static int GetXisfChannelCount(byte[] buffer)
        {
            Console.WriteLine("[GetXisfChannelCount] Warning: Using legacy byte array method - performance not optimized");
            return 1;
        }

        /// <summary>
        /// Optimized RGB rendering with single-pass statistics and parallel processing for performance
        /// </summary>
        private static byte[]? RenderXisfRgbOptimized(XisfHeaderCache cache, bool autoStretch = true)
        {
            if (cache.FileBytes == null || cache.Channels < 3)
                return null;
                
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int totalPixels = cache.Width * cache.Height;
            byte[] rgbPixels = new byte[totalPixels * 3]; // R,G,B for each pixel
            
            try
            {
                Console.WriteLine($"[RenderXisfRgbOptimized] Processing {cache.SampleFormat} data: {cache.Width}x{cache.Height}x{cache.Channels}");
                
                if (cache.SampleFormat.Equals("Float32", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessFloat32RgbOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, rgbPixels, autoStretch);
                }
                else if (cache.SampleFormat.Equals("UInt16", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessUInt16RgbOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, rgbPixels, autoStretch);
                }
                else if (cache.SampleFormat.Equals("UInt8", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessUInt8RgbOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, rgbPixels, autoStretch);
                }
                else
                {
                    throw new NotSupportedException($"Sample format {cache.SampleFormat} not supported in optimized renderer");
                }
                
                Console.WriteLine($"[RenderXisfRgbOptimized] Completed in {stopwatch.ElapsedMilliseconds}ms");
                return rgbPixels;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RenderXisfRgbOptimized] Error after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Optimized grayscale rendering with parallel processing
        /// </summary>
        private static byte[]? RenderXisfGrayscaleOptimized(XisfHeaderCache cache, bool autoStretch = true)
        {
            if (cache.FileBytes == null)
                return null;
                
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int totalPixels = cache.Width * cache.Height;
            byte[] pixels = new byte[totalPixels];
            
            try
            {
                Console.WriteLine($"[RenderXisfGrayscaleOptimized] Processing {cache.SampleFormat} data: {cache.Width}x{cache.Height}x{cache.Channels}");
                
                if (cache.SampleFormat.Equals("Float32", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessFloat32GrayscaleOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, pixels, autoStretch);
                }
                else if (cache.SampleFormat.Equals("UInt16", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessUInt16GrayscaleOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, pixels, autoStretch);
                }
                else if (cache.SampleFormat.Equals("UInt8", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessUInt8GrayscaleOptimized(cache.FileBytes, (int)cache.ImagePosition, cache.Width, cache.Height, cache.Channels, pixels, autoStretch);
                }
                else
                {
                    throw new NotSupportedException($"Sample format {cache.SampleFormat} not supported in optimized renderer");
                }
                
                Console.WriteLine($"[RenderXisfGrayscaleOptimized] Completed in {stopwatch.ElapsedMilliseconds}ms");
                return pixels;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RenderXisfGrayscaleOptimized] Error after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Optimized Float32 RGB processing with single-pass statistics and parallel processing
        /// </summary>
        private static void ProcessFloat32RgbOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] rgbPixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            int pixelsPerChannel = totalPixels;
            int bytesPerChannel = pixelsPerChannel * 4; // 4 bytes per Float32
            
            // Use parallel processing for channel statistics calculation
            var channelStats = new (float min, float max)[Math.Min(channels, 3)];
            
            Parallel.For(0, Math.Min(channels, 3), channelIndex =>
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                int channelOffset = offset + channelIndex * bytesPerChannel;
                
                // Single pass through channel data to find min/max
                for (int i = 0; i < pixelsPerChannel && channelOffset + i * 4 + 3 < buffer.Length; i++)
                {
                    float value = BitConverter.ToSingle(buffer, channelOffset + i * 4);
                    if (float.IsFinite(value))
                    {
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }
                }
                
                channelStats[channelIndex] = (min, max);
            });
            
            Console.WriteLine($"[ProcessFloat32RgbOptimized] Channel ranges - R: [{channelStats[0].min:F6}, {channelStats[0].max:F6}], G: [{channelStats[1].min:F6}, {channelStats[1].max:F6}], B: [{channelStats[2].min:F6}, {channelStats[2].max:F6}]");
            
            // Process pixels with optimized scaling using parallel processing
            Parallel.For(0, totalPixels, pixelIndex =>
            {
                byte r = 0, g = 0, b = 0;
                
                // Process each channel
                for (int c = 0; c < Math.Min(channels, 3); c++)
                {
                    int channelOffset = offset + c * bytesPerChannel + pixelIndex * 4;
                    if (channelOffset + 3 < buffer.Length)
                    {
                        float value = BitConverter.ToSingle(buffer, channelOffset);
                        if (float.IsFinite(value))
                        {
                            byte scaledValue;
                            if (autoStretch)
                            {
                                float range = channelStats[c].max - channelStats[c].min;
                                scaledValue = range > 0 
                                    ? (byte)Math.Clamp((int)((value - channelStats[c].min) * 255.0f / range), 0, 255)
                                    : (byte)Math.Clamp(channelStats[c].min, 0, 255);
                            }
                            else
                            {
                                // No stretching - use raw values, clamped to 0-255 range
                                scaledValue = (byte)Math.Clamp((int)(value * 255.0f), 0, 255);
                            }
                                
                            if (c == 0) r = scaledValue;      // Red
                            else if (c == 1) g = scaledValue; // Green
                            else if (c == 2) b = scaledValue; // Blue
                        }
                    }
                }
                
                // Write RGB values
                int rgbOffset = pixelIndex * 3;
                rgbPixels[rgbOffset] = r;
                rgbPixels[rgbOffset + 1] = g;
                rgbPixels[rgbOffset + 2] = b;
            });
        }
        
        /// <summary>
        /// Optimized UInt16 RGB processing with parallel processing
        /// </summary>
        private static void ProcessUInt16RgbOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] rgbPixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            int pixelsPerChannel = totalPixels;
            int bytesPerChannel = pixelsPerChannel * 2; // 2 bytes per UInt16
            
            // Parallel statistics calculation
            var channelStats = new (ushort min, ushort max)[Math.Min(channels, 3)];
            
            Parallel.For(0, Math.Min(channels, 3), channelIndex =>
            {
                ushort min = ushort.MaxValue;
                ushort max = ushort.MinValue;
                int channelOffset = offset + channelIndex * bytesPerChannel;
                
                for (int i = 0; i < pixelsPerChannel && channelOffset + i * 2 + 1 < buffer.Length; i++)
                {
                    ushort value = BitConverter.ToUInt16(buffer, channelOffset + i * 2);
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
                
                channelStats[channelIndex] = (min, max);
            });
            
            // Process pixels with parallel processing
            Parallel.For(0, totalPixels, pixelIndex =>
            {
                byte r = 0, g = 0, b = 0;
                
                for (int c = 0; c < Math.Min(channels, 3); c++)
                {
                    int channelOffset = offset + c * bytesPerChannel + pixelIndex * 2;
                    if (channelOffset + 1 < buffer.Length)
                    {
                        ushort value = BitConverter.ToUInt16(buffer, channelOffset);
                        byte scaledValue;
                        if (autoStretch)
                        {
                            ushort range = (ushort)(channelStats[c].max - channelStats[c].min);
                            scaledValue = range > 0 
                                ? (byte)((value - channelStats[c].min) * 255 / range)
                                : (byte)(channelStats[c].min >> 8);
                        }
                        else
                        {
                            // No stretching - just convert from 16-bit to 8-bit by dividing by 256
                            scaledValue = (byte)(value >> 8);
                        }
                            
                        if (c == 0) r = scaledValue;
                        else if (c == 1) g = scaledValue;
                        else if (c == 2) b = scaledValue;
                    }
                }
                
                int rgbOffset = pixelIndex * 3;
                rgbPixels[rgbOffset] = r;
                rgbPixels[rgbOffset + 1] = g;
                rgbPixels[rgbOffset + 2] = b;
            });
        }
        
        /// <summary>
        /// Optimized UInt8 RGB processing with parallel processing
        /// </summary>
        private static void ProcessUInt8RgbOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] rgbPixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            int pixelsPerChannel = totalPixels;
            
            Parallel.For(0, totalPixels, pixelIndex =>
            {
                byte r = 0, g = 0, b = 0;
                
                for (int c = 0; c < Math.Min(channels, 3); c++)
                {
                    int channelOffset = offset + c * pixelsPerChannel + pixelIndex;
                    if (channelOffset < buffer.Length)
                    {
                        byte value = buffer[channelOffset];
                        if (c == 0) r = value;
                        else if (c == 1) g = value;
                        else if (c == 2) b = value;
                    }
                }
                
                int rgbOffset = pixelIndex * 3;
                rgbPixels[rgbOffset] = r;
                rgbPixels[rgbOffset + 1] = g;
                rgbPixels[rgbOffset + 2] = b;
            });
        }
        
        /// <summary>
        /// Optimized Float32 grayscale processing with parallel processing
        /// </summary>
        private static void ProcessFloat32GrayscaleOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] pixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            
            if (channels == 1)
            {
                // Single channel - direct processing
                ProcessFloat32SingleChannel(buffer, offset, totalPixels, pixels, autoStretch);
            }
            else
            {
                // Multi-channel - luminance weighted average
                ProcessFloat32MultiChannelLuminance(buffer, offset, width, height, channels, pixels, autoStretch);
            }
        }
        
        private static void ProcessFloat32SingleChannel(byte[] buffer, int offset, int totalPixels, byte[] pixels, bool autoStretch = true)
        {
            float min = float.MaxValue, max = float.MinValue;
            
            // Single pass for min/max
            for (int i = 0; i < totalPixels && offset + i * 4 + 3 < buffer.Length; i++)
            {
                float value = BitConverter.ToSingle(buffer, offset + i * 4);
                if (float.IsFinite(value))
                {
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }
            
            float range = max - min;
            Parallel.For(0, totalPixels, i =>
            {
                if (offset + i * 4 + 3 < buffer.Length)
                {
                    float value = BitConverter.ToSingle(buffer, offset + i * 4);
                    if (float.IsFinite(value))
                    {
                        if (autoStretch)
                        {
                            pixels[i] = range > 0 
                                ? (byte)Math.Clamp((int)((value - min) * 255.0f / range), 0, 255)
                                : (byte)Math.Clamp(min, 0, 255);
                        }
                        else
                        {
                            // No stretching - use raw values, clamped to 0-255 range
                            pixels[i] = (byte)Math.Clamp((int)(value * 255.0f), 0, 255);
                        }
                    }
                }
            });
        }
        
        private static void ProcessFloat32MultiChannelLuminance(byte[] buffer, int offset, int width, int height, int channels, byte[] pixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            int pixelsPerChannel = totalPixels;
            int bytesPerChannel = pixelsPerChannel * 4;
            
            // Calculate luminance values and statistics in parallel
            float[] luminanceValues = new float[totalPixels];
            float min = float.MaxValue, max = float.MinValue;
            
            Parallel.For(0, totalPixels, pixelIndex =>
            {
                float r = 0, g = 0, b = 0;
                int validChannels = 0;
                
                for (int c = 0; c < Math.Min(channels, 3); c++)
                {
                    int channelOffset = offset + c * bytesPerChannel + pixelIndex * 4;
                    if (channelOffset + 3 < buffer.Length)
                    {
                        float value = BitConverter.ToSingle(buffer, channelOffset);
                        if (float.IsFinite(value))
                        {
                            if (c == 0) r = value;
                            else if (c == 1) g = value;
                            else if (c == 2) b = value;
                            validChannels++;
                        }
                    }
                }
                
                // ITU-R BT.709 luminance weights
                float luminance = validChannels >= 3 ? (0.2126f * r + 0.7152f * g + 0.0722f * b) : (r + g + b) / validChannels;
                luminanceValues[pixelIndex] = luminance;
            });
            
            // Find min/max
            for (int i = 0; i < totalPixels; i++)
            {
                if (luminanceValues[i] < min) min = luminanceValues[i];
                if (luminanceValues[i] > max) max = luminanceValues[i];
            }
            
            // Scale to bytes
            float range = max - min;
            Parallel.For(0, totalPixels, i =>
            {
                if (autoStretch)
                {
                    pixels[i] = range > 0 
                        ? (byte)Math.Clamp((int)((luminanceValues[i] - min) * 255.0f / range), 0, 255)
                        : (byte)Math.Clamp(min, 0, 255);
                }
                else
                {
                    // No stretching - use raw luminance values, clamped to 0-255 range
                    pixels[i] = (byte)Math.Clamp((int)(luminanceValues[i] * 255.0f), 0, 255);
                }
            });
        }
        
        /// <summary>
        /// Optimized UInt16 grayscale processing with parallel processing
        /// </summary>
        private static void ProcessUInt16GrayscaleOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] pixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            
            if (channels == 1)
            {
                // Single channel processing
                ushort min = ushort.MaxValue, max = ushort.MinValue;
                
                for (int i = 0; i < totalPixels && offset + i * 2 + 1 < buffer.Length; i++)
                {
                    ushort value = BitConverter.ToUInt16(buffer, offset + i * 2);
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
                
                ushort range = (ushort)(max - min);
                Parallel.For(0, totalPixels, i =>
                {
                    if (offset + i * 2 + 1 < buffer.Length)
                    {
                        ushort value = BitConverter.ToUInt16(buffer, offset + i * 2);
                        if (autoStretch)
                        {
                            pixels[i] = range > 0 
                                ? (byte)((value - min) * 255 / range)
                                : (byte)(min >> 8);
                        }
                        else
                        {
                            // No stretching - just convert from 16-bit to 8-bit by dividing by 256
                            pixels[i] = (byte)(value >> 8);
                        }
                    }
                });
            }
            else
            {
                // Multi-channel averaging
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 2;
                
                Parallel.For(0, totalPixels, pixelIndex =>
                {
                    int sum = 0;
                    int validChannels = 0;
                    
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * bytesPerChannel + pixelIndex * 2;
                        if (channelOffset + 1 < buffer.Length)
                        {
                            sum += BitConverter.ToUInt16(buffer, channelOffset);
                            validChannels++;
                        }
                    }
                    
                    pixels[pixelIndex] = validChannels > 0 
                        ? (byte)((sum / validChannels) >> 8)
                        : (byte)0;
                });
            }
        }
        
        /// <summary>
        /// Optimized UInt8 grayscale processing with parallel processing
        /// </summary>
        private static void ProcessUInt8GrayscaleOptimized(byte[] buffer, int offset, int width, int height, int channels, byte[] pixels, bool autoStretch = true)
        {
            int totalPixels = width * height;
            
            if (channels == 1)
            {
                // Direct copy for single channel
                Array.Copy(buffer, offset, pixels, 0, Math.Min(totalPixels, buffer.Length - offset));
            }
            else
            {
                // Multi-channel averaging
                int pixelsPerChannel = totalPixels;
                
                Parallel.For(0, totalPixels, pixelIndex =>
                {
                    int sum = 0;
                    int validChannels = 0;
                    
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * pixelsPerChannel + pixelIndex;
                        if (channelOffset < buffer.Length)
                        {
                            sum += buffer[channelOffset];
                            validChannels++;
                        }
                    }
                    
                    pixels[pixelIndex] = validChannels > 0 
                        ? (byte)(sum / validChannels)
                        : (byte)0;
                });
            }
        }

        public static System.Collections.Generic.Dictionary<string, object> GetXisfHeaders(string filePath)
        {
            try
            {
                var cache = GetOrCreateXisfCache(filePath);
                var metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["Image_Width"] = cache.Width,
                    ["Image_Height"] = cache.Height,
                    ["Image_Channels"] = cache.Channels,
                    ["Image_SampleFormat"] = cache.SampleFormat,
                    ["Image_geometry"] = $"{cache.Width}:{cache.Height}:{cache.Channels}",
                    ["Image_location"] = $"attachment:{cache.ImagePosition}:{cache.ImageSize}"
                };
                return metadata;
            }
            catch (Exception)
            {
                return new System.Collections.Generic.Dictionary<string, object>();
            }
        }
    }
}