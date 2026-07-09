using System;
using System.Windows.Media.Imaging;

namespace ApexAstro.Wpf.Services
{
    /// <summary>
    /// Service for generating histogram data from images
    /// </summary>
    public class HistogramService
    {
        private const int HistogramBins = 1024; // Increased from 256 for smoother histograms

        /// <summary>
        /// Generate histogram from raw byte image data (for FITS/XISF files before stretching)
        /// This analyzes the original data before any auto-stretch processing
        /// </summary>
        public int[] GenerateRawHistogram(byte[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                throw new ArgumentNullException(nameof(pixels));

            int[] histogram = new int[HistogramBins];

            // Map pixel values to histogram bins
            double binScale = (double)HistogramBins / 256.0;
            foreach (byte pixel in pixels)
            {
                int bin = Math.Min((int)(pixel * binScale), HistogramBins - 1);
                histogram[bin]++;
            }

            // Apply smoothing for better visualization
            histogram = SmoothHistogram(histogram);

            return histogram;
        }

        /// <summary>
        /// Generate histogram from raw floating-point image data (for FITS/XISF files)
        /// This analyzes the original data before any stretching or processing
        /// </summary>
        public int[] GenerateRawHistogram(double[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                throw new ArgumentNullException(nameof(pixels));

            int[] histogram = new int[HistogramBins];

            // Find min and max values in the raw data
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (double pixel in pixels)
            {
                if (pixel < min) min = pixel;
                if (pixel > max) max = pixel;
            }

            // Avoid division by zero
            double range = max - min;
            if (range < 0.0001)
            {
                // All pixels have same value
                histogram[HistogramBins / 2] = pixels.Length;
                return histogram;
            }

            // Map pixel values to histogram bins
            foreach (double pixel in pixels)
            {
                double normalized = (pixel - min) / range;
                int bin = (int)(normalized * (HistogramBins - 1));
                bin = Math.Max(0, Math.Min(HistogramBins - 1, bin));
                histogram[bin]++;
            }

            // Apply smoothing for better visualization
            histogram = SmoothHistogram(histogram);

            return histogram;
        }

        /// <summary>
        /// Generate histogram data for an RGB image
        /// </summary>
        public (int[] red, int[] green, int[] blue) GenerateRgbHistogram(BitmapSource bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int[] redHistogram = new int[HistogramBins];
            int[] greenHistogram = new int[HistogramBins];
            int[] blueHistogram = new int[HistogramBins];

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4; // Assuming 32-bit BGRA format
            byte[] pixels = new byte[height * stride];

            // Convert to Bgra32 format if not already
            BitmapSource formattedBitmap = bitmap;
            if (bitmap.Format != System.Windows.Media.PixelFormats.Bgra32 &&
                bitmap.Format != System.Windows.Media.PixelFormats.Bgr32)
            {
                formattedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            }

            formattedBitmap.CopyPixels(pixels, stride, 0);

            // Process each pixel - map 8-bit values to histogram bins
            double binScale = (double)HistogramBins / 256.0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte blue = pixels[i];
                byte green = pixels[i + 1];
                byte red = pixels[i + 2];
                // pixels[i + 3] is alpha, we ignore it

                int blueBin = Math.Min((int)(blue * binScale), HistogramBins - 1);
                int greenBin = Math.Min((int)(green * binScale), HistogramBins - 1);
                int redBin = Math.Min((int)(red * binScale), HistogramBins - 1);

                blueHistogram[blueBin]++;
                greenHistogram[greenBin]++;
                redHistogram[redBin]++;
            }

            // Apply smoothing to reduce spikiness
            redHistogram = SmoothHistogram(redHistogram);
            greenHistogram = SmoothHistogram(greenHistogram);
            blueHistogram = SmoothHistogram(blueHistogram);

            return (redHistogram, greenHistogram, blueHistogram);
        }

        /// <summary>
        /// Generate histogram data for a grayscale image
        /// </summary>
        public int[] GenerateGrayscaleHistogram(BitmapSource bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int[] histogram = new int[HistogramBins];

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // Convert to Gray8 format if possible, otherwise use RGB and average channels
            BitmapSource formattedBitmap;
            double binScale = (double)HistogramBins / 256.0;
            
            if (bitmap.Format == System.Windows.Media.PixelFormats.Gray8 ||
                bitmap.Format == System.Windows.Media.PixelFormats.Gray16 ||
                bitmap.Format == System.Windows.Media.PixelFormats.Gray32Float)
            {
                // Convert to Gray8 for consistency
                formattedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Gray8, null, 0);
                
                int stride = width;
                byte[] pixels = new byte[height * stride];
                formattedBitmap.CopyPixels(pixels, stride, 0);

                foreach (byte value in pixels)
                {
                    int bin = Math.Min((int)(value * binScale), HistogramBins - 1);
                    histogram[bin]++;
                }
            }
            else
            {
                // For color images, convert to grayscale using luminance formula
                formattedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                formattedBitmap.CopyPixels(pixels, stride, 0);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte blue = pixels[i];
                    byte green = pixels[i + 1];
                    byte red = pixels[i + 2];

                    // Standard luminance formula: 0.299*R + 0.587*G + 0.114*B
                    byte gray = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);
                    int bin = Math.Min((int)(gray * binScale), HistogramBins - 1);
                    histogram[bin]++;
                }
            }

            // Apply smoothing to reduce spikiness
            histogram = SmoothHistogram(histogram);

            return histogram;
        }

        /// <summary>
        /// Determine if an image should be treated as grayscale
        /// </summary>
        public bool IsGrayscaleImage(BitmapSource bitmap)
        {
            if (bitmap == null)
                return false;

            // Check pixel format
            var format = bitmap.Format;
            if (format == System.Windows.Media.PixelFormats.Gray8 ||
                format == System.Windows.Media.PixelFormats.Gray16 ||
                format == System.Windows.Media.PixelFormats.Gray32Float ||
                format == System.Windows.Media.PixelFormats.BlackWhite)
            {
                return true;
            }

            // For RGB images, sample some pixels to see if R=G=B
            // This is a heuristic check
            if (format == System.Windows.Media.PixelFormats.Bgra32 ||
                format == System.Windows.Media.PixelFormats.Bgr32 ||
                format == System.Windows.Media.PixelFormats.Bgr24)
            {
                int sampleSize = Math.Min(1000, bitmap.PixelWidth * bitmap.PixelHeight / 100);
                if (sampleSize < 10) return false;

                BitmapSource formattedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                int stride = bitmap.PixelWidth * 4;
                byte[] pixels = new byte[Math.Min(stride * bitmap.PixelHeight, stride * 100)]; // Sample first 100 rows
                formattedBitmap.CopyPixels(pixels, stride, 0);

                int grayscaleCount = 0;
                int totalSamples = 0;

                for (int i = 0; i < pixels.Length && totalSamples < sampleSize; i += 4)
                {
                    byte blue = pixels[i];
                    byte green = pixels[i + 1];
                    byte red = pixels[i + 2];

                    // Allow small variations (±2) for rounding errors
                    if (Math.Abs(red - green) <= 2 && Math.Abs(green - blue) <= 2)
                    {
                        grayscaleCount++;
                    }
                    totalSamples++;
                }

                // If more than 95% of sampled pixels have R=G=B, treat as grayscale
                return (double)grayscaleCount / totalSamples > 0.95;
            }

            return false;
        }

        /// <summary>
        /// Smooth histogram data using a Gaussian-like kernel to reduce spikiness
        /// </summary>
        private int[] SmoothHistogram(int[] histogram)
        {
            if (histogram == null || histogram.Length == 0)
                return histogram ?? Array.Empty<int>();

            int[] smoothed = new int[histogram.Length];
            
            // Use a 5-point smoothing kernel (weights: 1, 2, 3, 2, 1)
            // This provides gentle smoothing without losing too much detail
            for (int i = 0; i < histogram.Length; i++)
            {
                double sum = 0;
                double weightSum = 0;

                // Apply weighted average of neighboring bins
                for (int offset = -2; offset <= 2; offset++)
                {
                    int index = i + offset;
                    if (index >= 0 && index < histogram.Length)
                    {
                        double weight = 3 - Math.Abs(offset); // Weights: 1, 2, 3, 2, 1
                        sum += histogram[index] * weight;
                        weightSum += weight;
                    }
                }

                smoothed[i] = (int)(sum / weightSum);
            }

            return smoothed;
        }
    }
}
