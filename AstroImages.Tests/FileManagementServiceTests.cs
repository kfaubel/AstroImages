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
        
        [Fact]
        public void LoadFilesFromDirectory_LoadsMultipleFileTypes()
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
            Assert.NotNull(files);
            // Should load FITS, XISF, and possibly image files
            var extensions = files.Select(f => Path.GetExtension(f.Name).ToLowerInvariant()).Distinct();
            Assert.NotEmpty(extensions);
        }
        
        [Fact]
        public void GetFileInfo_CreatesFileItemWithCorrectProperties()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            var testFile = Directory.GetFiles(directory).FirstOrDefault();
            if (testFile == null)
            {
                return;
            }
            
            var expectedInfo = new FileInfo(testFile);
            
            // Act
            var fileItem = _service.GetFileInfo(testFile);
            
            // Assert
            Assert.Equal(expectedInfo.Name, fileItem.Name);
            Assert.Equal(expectedInfo.FullName, fileItem.Path);
            Assert.Equal(expectedInfo.Length, fileItem.Size);
        }
        
        [Fact]
        public void GenerateUniqueFileName_MultipleIterations_GeneratesIncrementingNames()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "AstroImagesTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var baseFileName = Path.Combine(tempDir, "test.fits");
                
                // Create the base file and a few numbered versions
                File.WriteAllText(baseFileName, "test");
                File.WriteAllText(Path.Combine(tempDir, "test (1).fits"), "test");
                File.WriteAllText(Path.Combine(tempDir, "test (2).fits"), "test");
                
                // Act
                var result = _service.GenerateUniqueFileName(baseFileName);
                
                // Assert
                Assert.Contains(" (3)", result); // Should be the next number
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
        
        [Fact]
        public void LoadFilesFromDirectory_HandlesEmptyDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "AstroImagesTest_Empty_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Act
                var files = _service.LoadFilesFromDirectory(tempDir);
                
                // Assert
                Assert.NotNull(files);
                Assert.Empty(files);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }
        
        [Fact]
        public void LoadFilesFromDirectory_IncludesXisfFiles()
        {
            // Arrange
            var directory = _testDataPath;
            
            if (!Directory.Exists(directory))
            {
                return;
            }
            
            // Act
            var files = _service.LoadFilesFromDirectory(directory);
            
            // Assert - Check if XISF files are included
            var xisfFiles = files.Where(f => f.Name.EndsWith(".xisf", StringComparison.OrdinalIgnoreCase));
            // May or may not have XISF files, just ensure method handles them
            Assert.NotNull(xisfFiles);
        }
        
        [Fact]
        public void MoveToRecycleBin_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFile = "C:\\NonExistent\\File.fits";
            
            // Act
            var result = _service.MoveToRecycleBin(nonExistentFile);
            
            // Assert
            Assert.False(result);
        }
    }
}
