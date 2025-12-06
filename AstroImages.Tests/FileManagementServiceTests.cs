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
    /// Tests for FileManagementService which handles file operations like loading, moving, and organizing files.
    /// </summary>
    public class FileManagementServiceTests
    {
        private readonly FileManagementService _service;
        private readonly string _testDataPath = Path.Combine("..", "..", "..", "..", "TestData2");
        
        public FileManagementServiceTests()
        {
            _service = new FileManagementService();
        }
        
        [Fact]
        public void LoadFilesFromDirectory_ValidDirectory_ReturnsFileList()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return; // Skip if test data doesn't exist
            }
            
            // Act
            var files = _service.LoadFilesFromDirectory(directory);
            
            // Assert
            Assert.NotNull(files);
            Assert.NotEmpty(files);
            Assert.All(files, file => 
            {
                Assert.NotNull(file.Name);
                Assert.NotNull(file.Path);
                Assert.True(file.Size >= 0);
            });
        }
        
        [Fact]
        public void LoadFilesFromDirectory_NonExistentDirectory_ReturnsEmptyList()
        {
            // Arrange
            var directory = "C:\\NonExistent\\Directory";
            
            // Act
            var files = _service.LoadFilesFromDirectory(directory);
            
            // Assert
            Assert.NotNull(files);
            Assert.Empty(files);
        }
        
        [Fact]
        public void LoadFilesFromDirectory_ValidDirectory_FilesSortedByName()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            // Act
            var files = _service.LoadFilesFromDirectory(directory);
            
            // Assert
            if (files.Count > 1)
            {
                var sortedNames = files.Select(f => f.Name).ToList();
                var expectedSorted = sortedNames.OrderBy(n => n).ToList();
                Assert.Equal(expectedSorted, sortedNames);
            }
        }
        
        [Fact]
        public void LoadFilesFromDirectory_LoadsFitsFiles()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            // Act
            var files = _service.LoadFilesFromDirectory(directory);
            
            // Assert
            var fitsFiles = files.Where(f => 
                f.Name.EndsWith(".fits", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith(".fit", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith(".fts", StringComparison.OrdinalIgnoreCase)
            );
            
            Assert.NotEmpty(fitsFiles);
        }
        
        [Fact]
        public void GetFileInfo_ValidFile_ReturnsFileItem()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            var files = Directory.GetFiles(directory, "*.fits").FirstOrDefault();
            if (files == null)
            {
                return;
            }
            
            // Act
            var fileItem = _service.GetFileInfo(files);
            
            // Assert
            Assert.NotNull(fileItem);
            Assert.NotNull(fileItem.Name);
            Assert.NotNull(fileItem.Path);
            Assert.True(fileItem.Size > 0);
        }
        
        [Fact]
        public void GenerateUniqueFileName_NonExistentFile_ReturnsSamePath()
        {
            // Arrange
            var filePath = "C:\\NonExistent\\test.fits";
            
            // Act
            var result = _service.GenerateUniqueFileName(filePath);
            
            // Assert
            Assert.Equal(filePath, result);
        }
        
        [Fact]
        public void GenerateUniqueFileName_ExistingFile_ReturnsUniqueFileName()
        {
            // Arrange - Use a file we know exists in TestData
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            var existingFile = Directory.GetFiles(directory, "*.fits").FirstOrDefault();
            if (existingFile == null)
            {
                return;
            }
            
            // Act
            var result = _service.GenerateUniqueFileName(existingFile);
            
            // Assert
            Assert.NotEqual(existingFile, result);
            Assert.Contains(" (1)", result);
        }
    }
}
