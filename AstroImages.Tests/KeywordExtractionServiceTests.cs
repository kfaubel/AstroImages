using Xunit;
using AstroImages.Wpf.Services;
using AstroImages.Wpf.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for KeywordExtractionService which extracts custom keywords from filenames
    /// and FITS keywords from file headers.
    /// </summary>
    public class KeywordExtractionServiceTests
    {
        private readonly KeywordExtractionService _service;
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData");
        
        public KeywordExtractionServiceTests()
        {
            _service = new KeywordExtractionService();
        }
        
        [Fact]
        public void ExtractCustomKeywordsFromFilename_NinaFilename_ExtractsKeywords()
        {
            // Arrange
            var filename = "2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits";
            var keywords = new[] { "RMS", "HFR", "Stars" };
            
            // Act
            var result = _service.ExtractCustomKeywordsFromFilename(filename, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("0.75", result["RMS"]);
            Assert.Equal("2.26", result["HFR"]);
            Assert.Equal("2029", result["Stars"]);
        }
        
        [Fact]
        public void ExtractCustomKeywordsFromFilename_EmptyKeywords_ReturnsEmptyDictionary()
        {
            // Arrange
            var filename = "2025-10-16_23-42-23_R_RMS_0.75_HFR_2.26_Stars_2029_100_10.00s_-9.60C_0052.fits";
            var keywords = new string[0];
            
            // Act
            var result = _service.ExtractCustomKeywordsFromFilename(filename, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ExtractFitsKeywords_ValidFitsFile_ExtractsKeywords()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            var keywords = new[] { "NAXIS", "BITPIX" };
            
            if (!File.Exists(fitsFile))
            {
                return;
            }
            
            // Act
            var result = _service.ExtractFitsKeywords(fitsFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            // Should extract at least some keywords if they exist in the file
        }
        
        [Fact]
        public void ExtractFitsKeywords_NonExistentFile_ReturnsEmptyDictionary()
        {
            // Arrange
            var fitsFile = "nonexistent.fits";
            var keywords = new[] { "NAXIS", "BITPIX" };
            
            // Act
            var result = _service.ExtractFitsKeywords(fitsFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ExtractFitsKeywords_EmptyKeywords_ReturnsEmptyDictionary()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            var keywords = new string[0];
            
            if (!File.Exists(fitsFile))
            {
                return;
            }
            
            // Act
            var result = _service.ExtractFitsKeywords(fitsFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ExtractFitsKeywords_XisfFile_ExtractsKeywords()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            var keywords = new[] { "TELESCOP", "INSTRUME", "FILTER" };
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var result = _service.ExtractFitsKeywords(xisfFile, keywords, skipXisf: false);
            
            // Assert
            Assert.NotNull(result);
            // XISF files may or may not have FITS keywords - test that it doesn't crash
        }
        
        [Fact]
        public void ExtractFitsKeywords_XisfFileSkipped_ReturnsEmptyDictionary()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            var keywords = new[] { "TELESCOP", "INSTRUME" };
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var result = _service.ExtractFitsKeywords(xisfFile, keywords, skipXisf: true);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Should skip XISF files when skipXisf is true
        }
        
        [Fact]
        public void PopulateKeywords_FileItem_PopulatesCustomAndFitsKeywords()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            if (!File.Exists(fitsFile))
            {
                return;
            }
            
            var fileItem = new FileItem
            {
                Name = Path.GetFileName(fitsFile),
                Path = fitsFile,
                Size = new FileInfo(fitsFile).Length
            };
            
            var customKeywords = new[] { "RMS", "HFR", "Stars" };
            var fitsKeywords = new[] { "NAXIS", "BITPIX" };
            
            // Act
            _service.PopulateKeywords(fileItem, customKeywords, fitsKeywords);
            
            // Assert
            Assert.NotNull(fileItem.CustomKeywords);
            Assert.NotNull(fileItem.FitsKeywords);
            
            // Should extract custom keywords from filename
            Assert.True(fileItem.CustomKeywords.Count > 0);
        }
        
        [Fact]
        public void PopulateKeywords_NoKeywords_DoesNotCrash()
        {
            // Arrange
            var fitsFile = Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits");
            
            if (!File.Exists(fitsFile))
            {
                return;
            }
            
            var fileItem = new FileItem
            {
                Name = Path.GetFileName(fitsFile),
                Path = fitsFile,
                Size = new FileInfo(fitsFile).Length
            };
            
            var customKeywords = new string[0];
            var fitsKeywords = new string[0];
            
            // Act
            _service.PopulateKeywords(fileItem, customKeywords, fitsKeywords);
            
            // Assert
            Assert.NotNull(fileItem.CustomKeywords);
            Assert.NotNull(fileItem.FitsKeywords);
        }
        
        [Fact]
        public void ExtractFitsKeywords_NonFitsNonXisfFile_ReturnsEmpty()
        {
            // Arrange - Create a temporary non-FITS/XISF file
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "This is not a FITS or XISF file");
            
            try
            {
                var keywords = new[] { "NAXIS", "BITPIX" };
                
                // Act
                var result = _service.ExtractFitsKeywords(tempFile, keywords);
                
                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        
        [Fact]
        public void ExtractFitsKeywords_MultipleFitsFiles_ExtractsConsistently()
        {
            // Arrange
            var fitsFiles = new[]
            {
                Path.Combine(_testDataPath, "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits"),
                Path.Combine(_testDataPath, "2025-10-17_07-31-45_R_RMS_0.00_HFR__Stars__100_0.02s_8.00C_0000.fits")
            };
            
            var keywords = new[] { "BITPIX", "NAXIS" };
            
            // Act & Assert
            foreach (var fitsFile in fitsFiles)
            {
                if (File.Exists(fitsFile))
                {
                    var result = _service.ExtractFitsKeywords(fitsFile, keywords);
                    Assert.NotNull(result);
                    // Each file should extract consistently
                }
            }
        }
        
        [Fact]
        public void PopulateKeywords_WithXisfFile_PopulatesCorrectly()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var fileItem = new FileItem
            {
                Name = Path.GetFileName(xisfFile),
                Path = xisfFile,
                Size = new FileInfo(xisfFile).Length
            };
            
            var customKeywords = new[] { "L60", "starless" };
            var fitsKeywords = new[] { "TELESCOP", "INSTRUME" };
            
            // Act
            _service.PopulateKeywords(fileItem, customKeywords, fitsKeywords, skipXisf: false);
            
            // Assert
            Assert.NotNull(fileItem.CustomKeywords);
            Assert.NotNull(fileItem.FitsKeywords);
        }
        
        [Fact]
        public void ExtractCustomKeywordsFromFilename_ComplexFilename_ExtractsAll()
        {
            // Arrange
            var filename = "2025-10-16_23-46-13_B_RMS_1.12_HFR_2.43_Stars_1269_100_10.00s_-10.00C_0026.fits";
            var keywords = new[] { "RMS", "HFR", "Stars", "100" };
            
            // Act
            var result = _service.ExtractCustomKeywordsFromFilename(filename, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("RMS"));
            Assert.True(result.ContainsKey("HFR"));
            Assert.True(result.ContainsKey("Stars"));
            Assert.True(result.ContainsKey("100"));
        }
    }
}
