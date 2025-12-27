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
                { "EXPTIME", 300.0 },
                { "OBJECT", "M31" },
                { "CCD-TEMP", -10.5 }
            };
            
            // Act
            var metadata = FitsUtilities.ExtractAstronomicalMetadata(headers);
            
            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.ContainsKey("TELESCOP"));
            Assert.Equal("Test Telescope", metadata["TELESCOP"]);
            Assert.True(metadata.ContainsKey("FILTER"));
            Assert.Equal("Luminance", metadata["FILTER"]);
        }
        
        [Fact]
        public void ExtractAstronomicalMetadata_EmptyHeaders_ReturnsEmptyMetadata()
        {
            // Arrange
            var headers = new Dictionary<string, object>();
            
            // Act
            var metadata = FitsUtilities.ExtractAstronomicalMetadata(headers);
            
            // Assert
            Assert.NotNull(metadata);
            Assert.Empty(metadata);
        }
        
        [Fact]
        public void GetFormatInfo_ValidHeaders_ReturnsFormatInfo()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "BITPIX", 16 },
                { "NAXIS", 2 },
                { "NAXIS1", 1920 },
                { "NAXIS2", 1080 },
                { "BZERO", 32768.0 },
                { "BSCALE", 1.0 }
            };
            
            // Act
            var formatInfo = FitsUtilities.GetFormatInfo(headers);
            
            // Assert
            Assert.NotNull(formatInfo);
            Assert.True(formatInfo.ContainsKey("BitPix"));
            Assert.Equal(16, formatInfo["BitPix"]);
            Assert.True(formatInfo.ContainsKey("Width"));
            Assert.Equal(1920, formatInfo["Width"]);
            Assert.True(formatInfo.ContainsKey("Height"));
            Assert.Equal(1080, formatInfo["Height"]);
            Assert.True(formatInfo.ContainsKey("DataType"));
        }
        
        [Fact]
        public void GetFormatInfo_MinimalHeaders_ReturnsPartialInfo()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "BITPIX", 8 }
            };
            
            // Act
            var formatInfo = FitsUtilities.GetFormatInfo(headers);
            
            // Assert
            Assert.NotNull(formatInfo);
            Assert.True(formatInfo.ContainsKey("BitPix"));
            Assert.True(formatInfo.ContainsKey("DataType"));
        }
        
        [Fact]
        public void FormatHeaderValue_Boolean_ReturnsFormattedString()
        {
            // Arrange
            var trueValue = true;
            var falseValue = false;
            
            // Act
            var trueResult = FitsUtilities.FormatHeaderValue(trueValue);
            var falseResult = FitsUtilities.FormatHeaderValue(falseValue);
            
            // Assert
            Assert.Equal("T", trueResult);
            Assert.Equal("F", falseResult);
        }
        
        [Fact]
        public void FormatHeaderValue_Numeric_ReturnsFormattedString()
        {
            // Arrange
            var intValue = 42;
            var doubleValue = 123.456;
            
            // Act
            var intResult = FitsUtilities.FormatHeaderValue(intValue);
            var doubleResult = FitsUtilities.FormatHeaderValue(doubleValue);
            
            // Assert
            Assert.Equal("42", intResult);
            Assert.NotNull(doubleResult);
        }
        
        [Fact]
        public void FormatHeaderValue_String_ReturnsTrimmedString()
        {
            // Arrange
            var stringValue = "  Test String  ";
            
            // Act
            var result = FitsUtilities.FormatHeaderValue(stringValue);
            
            // Assert
            Assert.Equal("Test String", result);
        }
        
        [Fact]
        public void FormatHeaderValue_Null_ReturnsEmptyString()
        {
            // Arrange
            object? nullValue = null;
            
            // Act
            var result = FitsUtilities.FormatHeaderValue(nullValue!);
            
            // Assert
            Assert.Equal("", result);
        }
        
        [Fact]
        public void ExtractWcsInfo_ValidHeaders_ExtractsWcsKeywords()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "CTYPE1", "RA---TAN" },
                { "CTYPE2", "DEC--TAN" },
                { "CRVAL1", 180.5 },
                { "CRVAL2", 45.3 },
                { "CRPIX1", 960.0 },
                { "CRPIX2", 540.0 },
                { "CDELT1", -0.0001 },
                { "CDELT2", 0.0001 }
            };
            
            // Act
            var wcsInfo = FitsUtilities.ExtractWcsInfo(headers);
            
            // Assert
            Assert.NotNull(wcsInfo);
            Assert.True(wcsInfo.ContainsKey("CTYPE1"));
            Assert.True(wcsInfo.ContainsKey("CRVAL1"));
            Assert.Equal("RA---TAN", wcsInfo["CTYPE1"]);
        }
        
        [Fact]
        public void ExtractWcsInfo_NoWcsHeaders_ReturnsEmptyDictionary()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "SIMPLE", true },
                { "BITPIX", 16 }
            };
            
            // Act
            var wcsInfo = FitsUtilities.ExtractWcsInfo(headers);
            
            // Assert
            Assert.NotNull(wcsInfo);
            Assert.Empty(wcsInfo);
        }
        
        [Fact]
        public void ValidateFitsHeader_ValidHeader_ReturnsNoIssues()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "SIMPLE", true },
                { "BITPIX", 16 },
                { "NAXIS", 2 },
                { "NAXIS1", 1920 },
                { "NAXIS2", 1080 }
            };
            
            // Act
            var issues = FitsUtilities.ValidateFitsHeader(headers);
            
            // Assert
            Assert.NotNull(issues);
            Assert.Empty(issues);
        }
        
        [Fact]
        public void ValidateFitsHeader_MissingRequiredKeyword_ReturnsIssue()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "SIMPLE", true },
                { "BITPIX", 16 }
                // Missing NAXIS
            };
            
            // Act
            var issues = FitsUtilities.ValidateFitsHeader(headers);
            
            // Assert
            Assert.NotNull(issues);
            Assert.NotEmpty(issues);
            Assert.Contains(issues, i => i.Contains("NAXIS"));
        }
        
        [Fact]
        public void ValidateFitsHeader_InvalidBitpix_ReturnsIssue()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "SIMPLE", true },
                { "BITPIX", 7 }, // Invalid BITPIX value
                { "NAXIS", 2 }
            };
            
            // Act
            var issues = FitsUtilities.ValidateFitsHeader(headers);
            
            // Assert
            Assert.NotNull(issues);
            Assert.Contains(issues, i => i.Contains("BITPIX"));
        }
        
        [Fact]
        public void ValidateFitsHeader_MissingNAXISn_ReturnsIssue()
        {
            // Arrange
            var headers = new Dictionary<string, object>
            {
                { "SIMPLE", true },
                { "BITPIX", 16 },
                { "NAXIS", 2 }
                // Missing NAXIS1 and NAXIS2
            };
            
            // Act
            var issues = FitsUtilities.ValidateFitsHeader(headers);
            
            // Assert
            Assert.NotNull(issues);
            Assert.Contains(issues, i => i.Contains("NAXIS1"));
            Assert.Contains(issues, i => i.Contains("NAXIS2"));
        }
    }
}
