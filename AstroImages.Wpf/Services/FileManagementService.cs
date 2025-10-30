using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AstroImages.Wpf.Models;

namespace AstroImages.Wpf.Services
{
    public class FileManagementService
    {
        public void MoveSelectedFiles(List<FileItem> selectedFiles, string targetDirectory, bool moveToTrash)
        {
            foreach (var file in selectedFiles)
            {
                try
                {
                    if (moveToTrash)
                    {
                        if (!MoveToRecycleBin(file.Path))
                        {
                            throw new InvalidOperationException($"Failed to move {file.Name} to recycle bin");
                        }
                    }
                    else
                    {
                        var destinationPath = Path.Combine(targetDirectory, file.Name);
                        
                        // Handle file overwrite by deleting existing file first
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }
                        
                        File.Move(file.Path, destinationPath);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error moving {file.Name}: {ex.Message}", ex);
                }
            }
        }

        public bool MoveToRecycleBin(string filePath)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath, 
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, 
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateUniqueFileName(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var directory = Path.GetDirectoryName(filePath) ?? "";
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;
            
            do
            {
                var newName = $"{nameWithoutExtension} ({counter}){extension}";
                newFilePath = Path.Combine(directory, newName);
                counter++;
            }
            while (File.Exists(newFilePath));

            return newFilePath;
        }

        public List<FileItem> LoadFilesFromDirectory(string directory)
        {
            var files = new List<FileItem>();
            
            if (!Directory.Exists(directory))
                return files;

            try
            {
                // Load all supported image formats
                var supportedExtensions = new[]
                {
                    "*.fits", "*.fit", "*.fts",  // FITS files
                    "*.xisf",                     // XISF files
                    "*.jpg", "*.jpeg",           // JPEG files
                    "*.png",                     // PNG files
                    "*.bmp",                     // Bitmap files
                    "*.tiff", "*.tif",           // TIFF files
                    "*.gif",                     // GIF files
                    "*.webp"                     // WebP files
                };

                var allFiles = new List<string>();
                foreach (var pattern in supportedExtensions)
                {
                    var matchingFiles = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
                    allFiles.AddRange(matchingFiles);
                }
                
                var fileInfos = allFiles.Select(f => new FileInfo(f))
                    .OrderBy(f => f.Name);

                foreach (var fileInfo in fileInfos)
                {
                    files.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Path = fileInfo.FullName,
                        Size = fileInfo.Length
                    });
                }
            }
            catch (Exception)
            {
                // If directory loading fails, return empty list
            }

            return files;
        }
    }
}