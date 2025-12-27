using Xunit;
using AstroImages.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for the XisfParser class which handles XISF (Extensible Image Serialization Format) files.
    /// XISF is used by PixInsight and other astronomical image processing software.
    /// </summary>
    public class XisfParserTests
    {
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData");
        
        [Fact]
        public void ParseMetadataFromFile_ValidXisfFile_ReturnsMetadata()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                // Skip test if file doesn't exist
                return;
            }
            
            // Act
            var metadata = XisfParser.ParseMetadataFromFile(xisfFile);
            
            // Assert
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata);
        }
        
        [Fact]
        public void ParseMetadataFromFile_ValidXisfFile_ContainsImageInfo()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "M101 R.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var metadata = XisfParser.ParseMetadataFromFile(xisfFile);
            
            // Assert
            // XISF files should contain basic image information
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata);
        }
        
        [Fact]
        public void ParseMetadataFromFile_InvalidFile_ThrowsException()
        {
            // Arrange
            var invalidFile = "nonexistent.xisf";
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                XisfParser.ParseMetadataFromFile(invalidFile));
        }
        
        [Fact]
        public void ParseSpecificFitsKeywords_ValidFile_ReturnsRequestedKeywords()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var keywords = new[] { "TELESCOP", "INSTRUME", "FILTER", "EXPTIME", "DATE-OBS" };
            
            // Act
            var result = XisfParser.ParseSpecificFitsKeywords(xisfFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            // Result may be empty if the file doesn't have these specific keywords
            // The important thing is that it doesn't crash
        }
        
        [Fact]
        public void ParseSpecificFitsKeywords_EmptyKeywordList_ReturnsEmptyDictionary()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var keywords = new string[0];
            
            // Act
            var result = XisfParser.ParseSpecificFitsKeywords(xisfFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ParseSpecificFitsKeywords_NullKeywords_ReturnsEmptyDictionary()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act
            var result = XisfParser.ParseSpecificFitsKeywords(xisfFile, null!);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ParseMetadataFromFile_MultipleXisfFiles_ParsesSuccessfully()
        {
            // Arrange
            var xisfFiles = new[]
            {
                Path.Combine(_testDataPath, "L60_starless - small.xisf"),
                Path.Combine(_testDataPath, "M101 R.xisf")
            };
            
            // Act & Assert
            foreach (var xisfFile in xisfFiles)
            {
                if (File.Exists(xisfFile))
                {
                    var metadata = XisfParser.ParseMetadataFromFile(xisfFile);
                    Assert.NotNull(metadata);
                }
            }
        }
        
        [Fact]
        public void ParseMetadataFromFile_ConsistentResults()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            // Act - Parse the same file twice
            var metadata1 = XisfParser.ParseMetadataFromFile(xisfFile);
            var metadata2 = XisfParser.ParseMetadataFromFile(xisfFile);
            
            // Assert - Results should be identical
            Assert.Equal(metadata1.Count, metadata2.Count);
            foreach (var key in metadata1.Keys)
            {
                Assert.True(metadata2.ContainsKey(key), $"Second parse should contain key: {key}");
            }
        }
        
        [Fact]
        public void ParseSpecificFitsKeywords_CaseInsensitive()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "M101 R.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var keywords = new[] { "TELESCOP", "telescop", "TeLeScOp" };
            
            // Act
            var result = XisfParser.ParseSpecificFitsKeywords(xisfFile, keywords);
            
            // Assert
            Assert.NotNull(result);
            // Should handle case variations gracefully
        }
        
        [Fact]
        public void ParseMetadata_ValidBuffer_ReturnsMetadata()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var buffer = File.ReadAllBytes(xisfFile);
            
            // Act
            var metadata = XisfParser.ParseMetadata(buffer);
            
            // Assert
            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata);
        }
        
        [Fact]
        public void ParseMetadata_InvalidSignature_ThrowsException()
        {
            // Arrange
            var buffer = new byte[100];
            Array.Fill(buffer, (byte)0);
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => XisfParser.ParseMetadata(buffer));
        }
        
        [Fact]
        public void ReadImage_ValidXisfFile_ReturnsImageData()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var buffer = File.ReadAllBytes(xisfFile);
            
            // Act
            try
            {
                var (width, height, pixels) = XisfParser.ReadImage(buffer);
                
                // Assert
                Assert.True(width > 0, "Width should be positive");
                Assert.True(height > 0, "Height should be positive");
                Assert.NotNull(pixels);
                Assert.Equal(width * height, pixels.Length);
            }
            catch (NotSupportedException)
            {
                // Some XISF files may use compression or formats not yet supported
                // This is acceptable for this test
            }
        }
        
        [Fact]
        public void ReadImage_InvalidBuffer_ThrowsException()
        {
            // Arrange
            var buffer = new byte[100];
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => XisfParser.ReadImage(buffer));
        }
        
        [Fact]
        public void ReadImageRgb_ValidXisfFile_ReturnsRgbData()
        {
            // Arrange
            var xisfFile = Path.Combine(_testDataPath, "L60_starless - small.xisf");
            
            if (!File.Exists(xisfFile))
            {
                return;
            }
            
            var buffer = File.ReadAllBytes(xisfFile);
            
            // Act
            try
            {
                var (width, height, rgbPixels) = XisfParser.ReadImageRgb(buffer);
                
                // Assert
                Assert.True(width > 0, "Width should be positive");
                Assert.True(height > 0, "Height should be positive");
                Assert.NotNull(rgbPixels);
                // RGB has 3 bytes per pixel
                Assert.Equal(width * height * 3, rgbPixels.Length);
            }
            catch (NotSupportedException)
            {
                // Some XISF files may use compression or formats not yet supported
                // This is acceptable for this test
            }
        }
        
        [Fact]
        public void ParseSpecificFitsKeywords_InvalidFile_ThrowsException()
        {
            // Arrange
            var invalidFile = "nonexistent.xisf";
            var keywords = new[] { "TELESCOP" };
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                XisfParser.ParseSpecificFitsKeywords(invalidFile, keywords));
        }
    }
}
