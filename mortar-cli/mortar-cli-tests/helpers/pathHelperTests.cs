using System.IO;
using mortarCli.helpers;
using Xunit;

namespace mortar_cli_tests.helpers
{
    public class pathHelperTests
    {
        // ── normalizePath ────────────────────────────────────────────────────

        [Fact]
        public void normalizePath_absolutePath_returnsNormalized()
        {
            string path = @"C:\Users\test\file.c";
            string result = pathHelper.normalizePath(path);
            Assert.NotNull(result);
            Assert.True(Path.IsPathRooted(result));
        }

        [Fact]
        public void normalizePath_null_returnsNull()
        {
            Assert.Null(pathHelper.normalizePath(null));
        }

        [Fact]
        public void normalizePath_whitespace_returnsNull()
        {
            Assert.Null(pathHelper.normalizePath("   "));
        }

        [Fact]
        public void normalizePath_relativePath_returnsAbsolute()
        {
            string result = pathHelper.normalizePath("somefile.c");
            Assert.NotNull(result);
            Assert.True(Path.IsPathRooted(result));
        }

        // ── pathsEqual ───────────────────────────────────────────────────────

        [Fact]
        public void pathsEqual_samePath_returnsTrue()
        {
            string path = @"C:\Users\test\file.c";
            Assert.True(pathHelper.pathsEqual(path, path));
        }

        [Fact]
        public void pathsEqual_differentCase_returnsTrue()
        {
            Assert.True(pathHelper.pathsEqual(
                @"C:\Users\Test\File.c",
                @"C:\Users\test\file.c"));
        }

        [Fact]
        public void pathsEqual_differentPaths_returnsFalse()
        {
            Assert.False(pathHelper.pathsEqual(
                @"C:\Users\test\file.c",
                @"C:\Users\test\other.c"));
        }

        [Fact]
        public void pathsEqual_nullInputs_returnsFalse()
        {
            Assert.False(pathHelper.pathsEqual(null, @"C:\file.c"));
            Assert.False(pathHelper.pathsEqual(@"C:\file.c", null));
            Assert.False(pathHelper.pathsEqual(null, null));
        }

        // ── makeRelativePath ─────────────────────────────────────────────────

        [Fact]
        public void makeRelativePath_fileInSubfolder_returnsRelative()
        {
            string basePath = @"C:\projects\myproject\";
            string fullPath = @"C:\projects\myproject\src\main.c";
            string result = pathHelper.makeRelativePath(basePath, fullPath);
            Assert.Equal(@"src\main.c", result);
        }

        [Fact]
        public void makeRelativePath_fileInRoot_returnsFilename()
        {
            string basePath = @"C:\projects\myproject\";
            string fullPath = @"C:\projects\myproject\main.c";
            string result = pathHelper.makeRelativePath(basePath, fullPath);
            Assert.Equal("main.c", result);
        }

        [Fact]
        public void makeRelativePath_nullBase_returnsFullPath()
        {
            string fullPath = @"C:\projects\myproject\main.c";
            string result = pathHelper.makeRelativePath(null, fullPath);
            Assert.Equal(fullPath, result);
        }

        // ── resolveRelativePath ──────────────────────────────────────────────

        [Fact]
        public void resolveRelativePath_relativePath_returnsAbsolute()
        {
            string basePath = @"C:\projects\myproject";
            string relative = @"src\main.c";
            string result = pathHelper.resolveRelativePath(basePath, relative);
            Assert.Equal(@"C:\projects\myproject\src\main.c", result);
        }

        [Fact]
        public void resolveRelativePath_absolutePath_returnsAsIs()
        {
            string basePath = @"C:\projects\myproject";
            string absolute = @"C:\other\file.c";
            string result = pathHelper.resolveRelativePath(basePath, absolute);
            Assert.Equal(absolute, result);
        }

        [Fact]
        public void resolveRelativePath_nullPath_returnsNull()
        {
            Assert.Null(pathHelper.resolveRelativePath(@"C:\base", null));
        }
    }
}