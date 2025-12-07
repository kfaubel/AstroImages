using Xunit;
using AstroImages.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for FitsUtilities which provides utility functions for FITS file operations
    /// and astronomical metadata processing.
    /// </summary>
    public class FitsUtilitiesTests
    {
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData");
        
        [Fact]
        public void IsFitsFile_ValidFitsFile_ReturnsTrue()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var result = FitsUtilities.IsFitsFile(fitsFile);
            
            // Assert
            Assert.True(result, "Valid FITS file should be recognized");
        }
        
        [Fact]
        public void IsFitsFile_NonFitsFile_ReturnsFalse()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var result = FitsUtilities.IsFitsFile(xisfFile);
            
            // Assert
            Assert.False(result, "XISF file should not be recognized as FITS");
        }
        
        [Fact]
        public void IsFitsFile_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFile = "nonexistent.fits";
            
            // Act
            var result = FitsUtilities.IsFitsFile(nonExistentFile);
            
            // Assert
            Assert.False(result, "Non-existent file should return false");
        }
        
        [Fact]
        public void IsFitsData_ValidFitsBuffer_ReturnsTrue()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            var buffer = File.ReadAllBytes(fitsFile);
            
            // Act
            var result = FitsUtilities.IsFitsData(buffer);
            
            // Assert
            Assert.True(result, "Valid FITS data should be recognized");
        }
        
        [Fact]
        public void IsFitsData_TooSmallBuffer_ReturnsFalse()
        {
            // Arrange
            var smallBuffer = new byte[40]; // Less than 80 bytes needed for FITS header
            
            // Act
            var result = FitsUtilities.IsFitsData(smallBuffer);
            
            // Assert
            Assert.False(result, "Too small buffer should return false");
        }
        
        [Fact]
        public void IsFitsData_NullBuffer_ReturnsFalse()
        {
            // Arrange
            byte[]? nullBuffer = null;
            
            // Act
            var result = FitsUtilities.IsFitsData(nullBuffer!);
            
            // Assert
            Assert.False(result, "Null buffer should return false");
        }
        
        [Fact]
        public void CalculateImageStatistics_ValidPixelData_ReturnsStatistics()
        {
            // Arrange
            var pixels = new byte[] { 0, 50, 100, 150, 200, 255 };
            
            // Act
            var stats = FitsUtilities.CalculateImageStatistics(pixels);
            
            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.ContainsKey("Count"), "Should contain Count");
            Assert.True(stats.ContainsKey("Mean"), "Should contain Mean");
            Assert.True(stats.ContainsKey("Min"), "Should contain Min");
            Assert.True(stats.ContainsKey("Max"), "Should contain Max");
            Assert.True(stats.ContainsKey("StdDev"), "Should contain StdDev");
            
            Assert.Equal(6, stats["Count"]);
            Assert.Equal(0, stats["Min"]);
            Assert.Equal(255, stats["Max"]);
        }
        
        [Fact]
        public void CalculateImageStatistics_UniformPixels_HasZeroStdDev()
        {
            // Arrange
            var pixels = new byte[] { 128, 128, 128, 128, 128 };
            
            // Act
            var stats = FitsUtilities.CalculateImageStatistics(pixels);
            
            // Assert
            Assert.Equal(128, stats["Mean"]);
            Assert.Equal(128, stats["Min"]);
            Assert.Equal(128, stats["Max"]);
            Assert.Equal(0, stats["StdDev"]);
        }
        
        [Fact]
        public void CalculateImageStatistics_EmptyArray_ReturnsEmptyDictionary()
        {
            // Arrange
            var pixels = new byte[0];
            
            // Act
            var stats = FitsUtilities.CalculateImageStatistics(pixels);
            
            // Assert
            Assert.NotNull(stats);
            Assert.Empty(stats);
        }
        
        [Fact]
        public void CalculateImageStatistics_NullArray_ReturnsEmptyDictionary()
        {
            // Arrange
            byte[]? pixels = null;
            
            // Act
            var stats = FitsUtilities.CalculateImageStatistics(pixels!);
            
            // Assert
            Assert.NotNull(stats);
            Assert.Empty(stats);
        }
        
        [Fact]
        public void ExtractAstronomicalMetadata_ValidHeaders_ExtractsMetadata()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "TELESCOP", "Test Telescope" },
                { "INSTRUME", "Test Camera" },
                { "FILTER", "Luminance" },
                { "EXPTIME", 300.0 }
            };
            
            // Act
            var metadata = FitsUtilities.ExtractAstronomicalMetadata(headers);
            
            // Assert
            Assert.NotNull(metadata);
            // Should extract common astronomical metadata
        }
        
        [Fact]
        public void ExtractAstronomicalMetadata_EmptyHeaders_ReturnsMetadata()
        {
            // Arrange
            var headers = new Dictionary<string, object>();
            
            // Act
            var metadata = FitsUtilities.ExtractAstronomicalMetadata(headers);
            
            // Assert
            Assert.NotNull(metadata);
        }
    }
}
