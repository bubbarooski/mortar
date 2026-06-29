using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using mortarCli.models;
using mortarCli.services;
namespace mortar_cli_tests.services
{
    public class storageServiceTests : IDisposable
    {
        private readonly string tempDir;
        private readonly string tempFile;

        public storageServiceTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            tempFile = Path.Combine(tempDir, "doclinks.mor");
        }

        public void Dispose()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        // ── loadLinks ────────────────────────────────────────────────────────

        [Fact]
        public void loadLinks_fileDoesNotExist_returnsEmptyList()
        {
            var result = storageService.loadLinks(tempFile);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void loadLinks_emptyFile_returnsEmptyList()
        {
            File.WriteAllText(tempFile, "");
            var result = storageService.loadLinks(tempFile);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void loadLinks_validJson_returnsLinks()
        {
            var links = new List<docLink>
            {
                new docLink
                {
                    sourceFile = @"src\main.c",
                    linkedAt = DateTime.UtcNow.ToString("o"),
                    documentPaths = new List<documentEntry>
                    {
                        new documentEntry
                        {
                            path = @"C:\docs\datasheet.pdf",
                            nickname = "IMU Sheet",
                            docType = "datasheet"
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(links, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(tempFile, json);

            var result = storageService.loadLinks(tempFile);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(@"src\main.c", result[0].sourceFile);
            Assert.Single(result[0].documentPaths);
            Assert.Equal("IMU Sheet", result[0].documentPaths[0].nickname);
        }

        [Fact]
        public void loadLinks_corruptedJson_returnsNull()
        {
            File.WriteAllText(tempFile, "{ this is not valid json {{{{");
            var result = storageService.loadLinks(tempFile);
            Assert.Null(result);
        }

        // ── saveLinks ────────────────────────────────────────────────────────

        [Fact]
        public void saveLinks_validLinks_writesFile()
        {
            var links = new List<docLink>
            {
                new docLink
                {
                    sourceFile = @"src\main.c",
                    linkedAt = DateTime.UtcNow.ToString("o"),
                    documentPaths = new List<documentEntry>
                    {
                        new documentEntry { path = @"C:\docs\sheet.pdf" }
                    }
                }
            };

            bool result = storageService.saveLinks(tempFile, links);
            Assert.True(result);
            Assert.True(File.Exists(tempFile));
        }

        [Fact]
        public void saveLinks_thenLoad_roundTrips()
        {
            var links = new List<docLink>
            {
                new docLink
                {
                    sourceFile = @"src\sensor.c",
                    linkedAt = DateTime.UtcNow.ToString("o"),
                    documentPaths = new List<documentEntry>
                    {
                        new documentEntry
                        {
                            path = @"C:\docs\sensor_datasheet.pdf",
                            nickname = "Sensor Sheet",
                            docType = "datasheet",
                            isPrimary = true,
                            outOfDateDetection = true
                        }
                    }
                }
            };

            storageService.saveLinks(tempFile, links);
            var loaded = storageService.loadLinks(tempFile);

            Assert.NotNull(loaded);
            Assert.Single(loaded);
            Assert.Equal(@"src\sensor.c", loaded[0].sourceFile);
            Assert.Equal("Sensor Sheet", loaded[0].documentPaths[0].nickname);
            Assert.Equal("datasheet", loaded[0].documentPaths[0].docType);
            Assert.True(loaded[0].documentPaths[0].isPrimary);
        }

        [Fact]
        public void saveLinks_emptyList_writesEmptyArray()
        {
            bool result = storageService.saveLinks(tempFile, new List<docLink>());
            Assert.True(result);

            var loaded = storageService.loadLinks(tempFile);
            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }

        [Fact]
        public void saveLinks_multipleEntries_preservesOrder()
        {
            var links = new List<docLink>
            {
                new docLink { sourceFile = @"src\a.c", linkedAt = DateTime.UtcNow.ToString("o"), documentPaths = new List<documentEntry> { new documentEntry { path = @"C:\a.pdf" } } },
                new docLink { sourceFile = @"src\b.c", linkedAt = DateTime.UtcNow.ToString("o"), documentPaths = new List<documentEntry> { new documentEntry { path = @"C:\b.pdf" } } },
                new docLink { sourceFile = @"src\c.c", linkedAt = DateTime.UtcNow.ToString("o"), documentPaths = new List<documentEntry> { new documentEntry { path = @"C:\c.pdf" } } }
            };

            storageService.saveLinks(tempFile, links);
            var loaded = storageService.loadLinks(tempFile);

            Assert.Equal(3, loaded.Count);
            Assert.Equal(@"src\a.c", loaded[0].sourceFile);
            Assert.Equal(@"src\b.c", loaded[1].sourceFile);
            Assert.Equal(@"src\c.c", loaded[2].sourceFile);
        }
    }
}