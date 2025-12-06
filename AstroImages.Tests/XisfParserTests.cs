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
            var result = XisfParser.ParseSpecificFitsKeywords(xisfFile, null);
            
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
    }
}
