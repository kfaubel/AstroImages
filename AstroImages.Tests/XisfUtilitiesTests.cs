using Xunit;
using AstroImages.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for XisfUtilities which provides utility functions for XISF file operations.
    /// </summary>
    public class XisfUtilitiesTests
    {
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData");
        
        [Fact]
        public void IsXisfFile_ValidXisfFile_ReturnsTrue()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var result = XisfUtilities.IsXisfFile(xisfFile);
            
            // Assert
            Assert.True(result, "Valid XISF file should be recognized");
        }
        
        [Fact]
        public void IsXisfFile_FitsFile_ReturnsFalse()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var result = XisfUtilities.IsXisfFile(fitsFile);
            
            // Assert
            Assert.False(result, "FITS file should not be recognized as XISF");
        }
        
        [Fact]
        public void IsXisfFile_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFile = "nonexistent.xisf";
            
            // Act
            var result = XisfUtilities.IsXisfFile(nonExistentFile);
            
            // Assert
            Assert.False(result, "Non-existent file should return false");
        }
        
        [Fact]
        public void HasXisfExtension_XisfFile_ReturnsTrue()
        {
            // Arrange
            var filename = "test.xisf";
            
            // Act
            var result = XisfUtilities.HasXisfExtension(filename);
            
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public void HasXisfExtension_XisfFileUpperCase_ReturnsTrue()
        {
            // Arrange
            var filename = "test.XISF";
            
            // Act
            var result = XisfUtilities.HasXisfExtension(filename);
            
            // Assert
            Assert.True(result, "Should be case-insensitive");
        }
        
        [Fact]
        public void HasXisfExtension_FitsFile_ReturnsFalse()
        {
            // Arrange
            var filename = "test.fits";
            
            // Act
            var result = XisfUtilities.HasXisfExtension(filename);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void HasXisfExtension_EmptyString_ReturnsFalse()
        {
            // Arrange
            var filename = "";
            
            // Act
            var result = XisfUtilities.HasXisfExtension(filename);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void HasXisfExtension_NullPath_ReturnsFalse()
        {
            // Arrange
            string? nullPath = null;
            
            // Act
            var result = XisfUtilities.HasXisfExtension(nullPath!);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void IsXisfData_ValidXisfBuffer_ReturnsTrue()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var buffer = File.ReadAllBytes(xisfFile);
            
            // Act
            var result = XisfUtilities.IsXisfData(buffer);
            
            // Assert
            Assert.True(result, "Valid XISF data should be recognized");
        }
        
        [Fact]
        public void IsXisfData_TooSmallBuffer_ReturnsFalse()
        {
            // Arrange
            var smallBuffer = new byte[4]; // Less than 8 bytes needed for XISF signature
            
            // Act
            var result = XisfUtilities.IsXisfData(smallBuffer);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void IsXisfData_NullBuffer_ReturnsFalse()
        {
            // Arrange
            byte[]? nullBuffer = null;
            
            // Act
            var result = XisfUtilities.IsXisfData(nullBuffer!);
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void CalculateImageStatistics_ValidPixelData_ReturnsStatistics()
        {
            // Arrange
            var pixels = new byte[] { 10, 20, 30, 40, 50 };
            
            // Act
            var stats = XisfUtilities.CalculateImageStatistics(pixels);
            
            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.ContainsKey("Count"));
            Assert.True(stats.ContainsKey("Mean"));
            Assert.True(stats.ContainsKey("Min"));
            Assert.True(stats.ContainsKey("Max"));
            Assert.True(stats.ContainsKey("StdDev"));
            
            Assert.Equal(5, stats["Count"]);
            Assert.Equal(30, stats["Mean"]);
            Assert.Equal(10, stats["Min"]);
            Assert.Equal(50, stats["Max"]);
        }
        
        [Fact]
        public void CalculateImageStatistics_EmptyArray_ReturnsEmptyDictionary()
        {
            // Arrange
            var pixels = new byte[0];
            
            // Act
            var stats = XisfUtilities.CalculateImageStatistics(pixels);
            
            // Assert
            Assert.NotNull(stats);
            Assert.Empty(stats);
        }
    }
}
