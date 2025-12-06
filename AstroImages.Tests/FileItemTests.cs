using Xunit;
using AstroImages.Wpf.Models;
using System.ComponentModel;

namespace AstroImages.Tests
{
    /// <summary>
    /// Tests for the FileItem model which represents an image file with metadata.
    /// Tests INotifyPropertyChanged implementation and property behaviors.
    /// </summary>
    public class FileItemTests
    {
        [Fact]
        public void FileItem_SetIsSelected_RaisesPropertyChanged()
        {
            // Arrange
            var fileItem = new FileItem();
            var propertyChangedRaised = false;
            string changedPropertyName = null;
            
            fileItem.PropertyChanged += (sender, e) =>
            {
                propertyChangedRaised = true;
                changedPropertyName = e.PropertyName;
            };
            
            // Act
            fileItem.IsSelected = true;
            
            // Assert
            Assert.True(propertyChangedRaised, "PropertyChanged should be raised");
            Assert.Equal(nameof(FileItem.IsSelected), changedPropertyName);
        }
        
        [Fact]
        public void FileItem_SetIsSelectedSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var fileItem = new FileItem();
            fileItem.IsSelected = true;
            
            var propertyChangedRaised = false;
            fileItem.PropertyChanged += (sender, e) =>
            {
                propertyChangedRaised = true;
            };
            
            // Act
            fileItem.IsSelected = true; // Same value
            
            // Assert
            Assert.False(propertyChangedRaised, "PropertyChanged should not be raised for same value");
        }
        
        [Fact]
        public void FileItem_SetCustomKeywords_RaisesPropertyChanged()
        {
            // Arrange
            var fileItem = new FileItem();
            var propertyChangedRaised = false;
            
            fileItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(FileItem.CustomKeywords))
                {
                    propertyChangedRaised = true;
                }
            };
            
            // Act
            fileItem.CustomKeywords = new System.Collections.Generic.Dictionary<string, string>
            {
                { "RMS", "0.75" },
                { "HFR", "2.26" }
            };
            
            // Assert
            Assert.True(propertyChangedRaised, "PropertyChanged should be raised for CustomKeywords");
        }
        
        [Fact]
        public void FileItem_SetFitsKeywords_RaisesPropertyChanged()
        {
            // Arrange
            var fileItem = new FileItem();
            var propertyChangedRaised = false;
            
            fileItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(FileItem.FitsKeywords))
                {
                    propertyChangedRaised = true;
                }
            };
            
            // Act
            fileItem.FitsKeywords = new System.Collections.Generic.Dictionary<string, string>
            {
                { "NAXIS", "2" },
                { "BITPIX", "16" }
            };
            
            // Assert
            Assert.True(propertyChangedRaised, "PropertyChanged should be raised for FitsKeywords");
        }
        
        [Fact]
        public void FileItem_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var fileItem = new FileItem();
            
            // Assert
            Assert.Equal(string.Empty, fileItem.Name);
            Assert.Equal(string.Empty, fileItem.Path);
            Assert.Equal(0, fileItem.Size);
            Assert.False(fileItem.IsSelected);
            Assert.NotNull(fileItem.CustomKeywords);
            Assert.NotNull(fileItem.FitsKeywords);
        }
        
        [Fact]
        public void FileItem_SetProperties_StoresValues()
        {
            // Arrange
            var fileItem = new FileItem();
            
            // Act
            fileItem.Name = "test.fits";
            fileItem.Path = "C:\\test\\test.fits";
            fileItem.Size = 1024;
            fileItem.IsSelected = true;
            
            // Assert
            Assert.Equal("test.fits", fileItem.Name);
            Assert.Equal("C:\\test\\test.fits", fileItem.Path);
            Assert.Equal(1024, fileItem.Size);
            Assert.True(fileItem.IsSelected);
        }
        
        [Fact]
        public void FileItem_ImplementsINotifyPropertyChanged()
        {
            // Arrange
            var fileItem = new FileItem();
            
            // Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(fileItem);
        }
    }
}
