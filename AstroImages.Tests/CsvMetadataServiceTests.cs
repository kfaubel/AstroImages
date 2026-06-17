using System;
using System.Collections.Generic;
using System.IO;
using AstroImages.Wpf.Services;
using Xunit;

namespace AstroImages.Tests
{
    public class CsvMetadataServiceTests
    {
        [Fact]
        public void GetValues_WithImageAndAcquisitionFiles_ReturnsMergedValues()
        {
            var folder = CreateTempFolder();
            try
            {
                File.WriteAllText(
                    Path.Combine(folder, "ImageMetaData.csv"),
                    "FilePath,HFR,FilterName\n" +
                    "C:/data/light_0001.fits,2.2647,L\n");

                File.WriteAllText(
                    Path.Combine(folder, "AcquisitionDetails.csv"),
                    "TargetName,RACoordinates,TelescopeName\n" +
                    "M 16,18h 18m 48s,CarbonStar 200\n");

                var service = new CsvMetadataService();
                service.Load(folder);

                var values = service.GetValues("light_0001.fits", new[] { "HFR", "TargetName", "TelescopeName" });

                Assert.Equal("2.26", values["HFR"]);
                Assert.Equal("M 16", values["TargetName"]);
                Assert.Equal("CarbonStar 200", values["TelescopeName"]);
            }
            finally
            {
                DeleteTempFolder(folder);
            }
        }

        [Fact]
        public void GetValues_WhenFileRowMissing_ReturnsSessionLevelAcquisitionValues()
        {
            var folder = CreateTempFolder();
            try
            {
                File.WriteAllText(
                    Path.Combine(folder, "AcquisitionDetails.csv"),
                    "TargetName,ObserverLatitude\n" +
                    "M 101,31.5469\n");

                var service = new CsvMetadataService();
                service.Load(folder);

                var values = service.GetValues("does_not_exist.fits", new[] { "TargetName", "ObserverLatitude" });

                Assert.Equal("M 101", values["TargetName"]);
                Assert.Equal("32", values["ObserverLatitude"]);
            }
            finally
            {
                DeleteTempFolder(folder);
            }
        }

        [Fact]
        public void GetValues_AcquisitionDetailsWithFilePath_MergesPerFileValues()
        {
            var folder = CreateTempFolder();
            try
            {
                File.WriteAllText(
                    Path.Combine(folder, "AcquisitionDetails.csv"),
                    "FilePath,TargetName,TelescopeName\n" +
                    "C:/data/light_0002.fits,IC 5070,My Scope\n");

                var service = new CsvMetadataService();
                service.Load(folder);

                var values = service.GetValues("light_0002.fits", new[] { "TargetName", "TelescopeName" });

                Assert.Equal("IC 5070", values["TargetName"]);
                Assert.Equal("My Scope", values["TelescopeName"]);
            }
            finally
            {
                DeleteTempFolder(folder);
            }
        }

        private static string CreateTempFolder()
        {
            var folder = Path.Combine(Path.GetTempPath(), "AstroImagesTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void DeleteTempFolder(string folder)
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
