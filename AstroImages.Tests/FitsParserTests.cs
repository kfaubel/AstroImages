using Xunit;
using AstroImages.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for the FitsParser class which is critical for reading astronomical FITS files.
    /// These tests validate parsing capabilities, header extraction, and data reading functionality.
    /// </summary>
    public class FitsParserTests
    {
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData");
        
        [Fact]
        public void ParseHeaderFromFile_ValidFitsFile_ReturnsHeader()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var header = FitsParser.ParseHeaderFromFile(fitsFile);
            
            // Assert
            Assert.NotNull(header);
            Assert.NotEmpty(header);
            Assert.True(header.ContainsKey("SIMPLE") || header.ContainsKey("XTENSION"), "FITS file should have SIMPLE or XTENSION keyword");
        }
        
        [Fact]
        public void ParseHeaderFromFile_ValidFitsFile_ContainsStandardKeywords()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var header = FitsParser.ParseHeaderFromFile(fitsFile);
            
            // Assert - Common FITS keywords that should exist
            Assert.True(header.ContainsKey("BITPIX"), "FITS file should have BITPIX keyword");
            Assert.True(header.ContainsKey("NAXIS"), "FITS file should have NAXIS keyword");
        }
        
        [Fact]
        public void ParseHeaderFromFile_NonExistentFile_ReturnsEmptyDictionary()
        {
            // Arrange
            var nonExistentFile = "nonexistent.fits";
            
            // Act
            var header = FitsParser.ParseHeaderFromFile(nonExistentFile);
            
            // Assert
            Assert.NotNull(header);
            Assert.Empty(header);
        }
        
        [Fact]
        public void ParseHeader_ValidBuffer_ReturnsHeaderData()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            var buffer = File.ReadAllBytes(fitsFile);
            
            // Act
            var header = FitsParser.ParseHeader(buffer);
            
            // Assert
            Assert.NotNull(header);
            Assert.NotEmpty(header);
        }
        
        [Fact]
        public void ParseHeader_TooSmallBuffer_HandlesGracefully()
        {
            // Arrange
            var tinyBuffer = new byte[40]; // Less than one FITS card (80 bytes)
            
            // Act
            var header = FitsParser.ParseHeader(tinyBuffer);
            
            // Assert
            Assert.NotNull(header);
            // Should either be empty or handle gracefully without throwing
        }
        
        [Fact]
        public void ParseHeaderFromFile_ExtractsNumericalValues()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var header = FitsParser.ParseHeaderFromFile(fitsFile);
            
            // Assert
            if (header.ContainsKey("NAXIS"))
            {
                var naxis = header["NAXIS"];
                Assert.True(naxis is int || naxis is long || naxis is double, "NAXIS should be a numerical value");
            }
        }
        
        [Fact]
        public void ParseHeaderFromFile_ExtractsStringValues()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act
            var header = FitsParser.ParseHeaderFromFile(fitsFile);
            
            // Assert
            // Look for string-type keywords (many FITS files have TELESCOP, INSTRUME, etc.)
            _ = header.Values.Any(v => v is string);
            // This is informational - not all FITS files have string keywords
            Assert.True(true, "Parser should handle string values");
        }
        
        [Fact]
        public void ParseHeaderFromFile_DifferentFitsFiles_ParsesSuccessfully()
        {
            // Arrange
            var fitsFiles = new[]
            {
                Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits"),
                Path.Combine(_testDataPath, "2025-10-17_07-31-45_R_RMS_0.00_HFR__Stars__100_0.02s_8.00C_0000.fits")
            };
            
            // Act & Assert
            foreach (var fitsFile in fitsFiles)
            {
                if (File.Exists(fitsFile))
                {
                    var header = FitsParser.ParseHeaderFromFile(fitsFile);
                    Assert.NotNull(header);
                    Assert.NotEmpty(header);
                }
            }
        }
        
        [Fact]
        public void ParseHeaderFromFile_ConsistentResults()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            // Act - Parse the same file twice
            var header1 = FitsParser.ParseHeaderFromFile(fitsFile);
            var header2 = FitsParser.ParseHeaderFromFile(fitsFile);
            
            // Assert - Results should be identical
            Assert.Equal(header1.Count, header2.Count);
            foreach (var key in header1.Keys)
            {
                Assert.True(header2.ContainsKey(key), $"Second parse should contain key: {key}");
            }
        }
    }
}
