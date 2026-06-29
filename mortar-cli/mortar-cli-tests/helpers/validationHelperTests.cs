using mortarCli.helpers;
using Xunit;

namespace mortar_cli_tests.helpers
{
    public class validationHelperTests
    {
        // ── isValidUrl ───────────────────────────────────────────────────────

        [Fact]
        public void isValidUrl_httpUrl_returnsTrue()
        {
            Assert.True(validationHelper.isValidUrl("http://example.com"));
        }

        [Fact]
        public void isValidUrl_httpsUrl_returnsTrue()
        {
            Assert.True(validationHelper.isValidUrl("https://example.com/docs/sheet.pdf"));
        }

        [Fact]
        public void isValidUrl_noScheme_returnsFalse()
        {
            Assert.False(validationHelper.isValidUrl("example.com"));
        }

        [Fact]
        public void isValidUrl_ftpScheme_returnsFalse()
        {
            Assert.False(validationHelper.isValidUrl("ftp://example.com"));
        }

        [Fact]
        public void isValidUrl_null_returnsFalse()
        {
            Assert.False(validationHelper.isValidUrl(null));
        }

        [Fact]
        public void isValidUrl_whitespace_returnsFalse()
        {
            Assert.False(validationHelper.isValidUrl("   "));
        }

        // ── isValidDocType ───────────────────────────────────────────────────

        [Fact]
        public void isValidDocType_validType_returnsTrue()
        {
            Assert.True(validationHelper.isValidDocType("datasheet"));
            Assert.True(validationHelper.isValidDocType("requirements"));
            Assert.True(validationHelper.isValidDocType("schematic"));
            Assert.True(validationHelper.isValidDocType("testSpec"));
            Assert.True(validationHelper.isValidDocType("apiSpec"));
            Assert.True(validationHelper.isValidDocType("researchPaper"));
            Assert.True(validationHelper.isValidDocType("designSpec"));
            Assert.True(validationHelper.isValidDocType("runbook"));
            Assert.True(validationHelper.isValidDocType("license"));
            Assert.True(validationHelper.isValidDocType("changelog"));
            Assert.True(validationHelper.isValidDocType("other"));
        }

        [Fact]
        public void isValidDocType_caseInsensitive_returnsTrue()
        {
            Assert.True(validationHelper.isValidDocType("DATASHEET"));
            Assert.True(validationHelper.isValidDocType("DataSheet"));
        }

        [Fact]
        public void isValidDocType_invalidType_returnsFalse()
        {
            Assert.False(validationHelper.isValidDocType("manual"));
            Assert.False(validationHelper.isValidDocType("pdf"));
        }

        [Fact]
        public void isValidDocType_null_returnsFalse()
        {
            Assert.False(validationHelper.isValidDocType(null));
        }

        [Fact]
        public void isValidDocType_whitespace_returnsFalse()
        {
            Assert.False(validationHelper.isValidDocType("   "));
        }

        // ── isValidNickname ──────────────────────────────────────────────────

        [Fact]
        public void isValidNickname_validName_returnsTrue()
        {
            Assert.True(validationHelper.isValidNickname("IMU Datasheet"));
        }

        [Fact]
        public void isValidNickname_null_returnsFalse()
        {
            Assert.False(validationHelper.isValidNickname(null));
        }

        [Fact]
        public void isValidNickname_whitespace_returnsFalse()
        {
            Assert.False(validationHelper.isValidNickname("   "));
        }

        // ── validateEntry ────────────────────────────────────────────────────

        [Fact]
        public void validateEntry_withPath_returnsNull()
        {
            Assert.Null(validationHelper.validateEntry(@"C:\docs\sheet.pdf", null));
        }

        [Fact]
        public void validateEntry_withUrl_returnsNull()
        {
            Assert.Null(validationHelper.validateEntry(null, "https://example.com"));
        }

        [Fact]
        public void validateEntry_bothNull_returnsError()
        {
            string result = validationHelper.validateEntry(null, null);
            Assert.NotNull(result);
        }

        [Fact]
        public void validateEntry_invalidUrl_returnsError()
        {
            string result = validationHelper.validateEntry(null, "not-a-url");
            Assert.NotNull(result);
        }

        [Fact]
        public void validateEntry_bothEmpty_returnsError()
        {
            string result = validationHelper.validateEntry("", "");
            Assert.NotNull(result);
        }
    }
}