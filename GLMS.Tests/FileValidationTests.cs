// Part 3: Updated to use GLMS.Api namespace (Services moved from GLMS.Web to GLMS.Api in the SOA refactor)
using GLMS.Api.Services;
using Xunit;

namespace GLMS.Tests
{
    /// <summary>
    /// UNIT TESTS — File Validation (xUnit)
    ///
    /// Tests the FileValidationService in isolation — no web server needed.
    ///
    /// Requirement from brief:
    ///   "File Validation: Verify that uploading a restricted file type
    ///    (e.g., .exe) throws an error (only .pdf allowed)."
    /// </summary>
    public class FileValidationTests
    {
        private readonly FileValidationService _service = new FileValidationService();

        // =====================================================================
        // Tests using the string-based overload (testable without IFormFile)
        // =====================================================================

        [Fact]
        public void IsValidPdf_ReturnsTrue_ForPdfFile()
        {
            // Arrange
            string fileName    = "signed_agreement.pdf";
            string contentType = "application/pdf";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_ForExeFile()
        {
            // Arrange — the exact scenario described in the brief
            string fileName    = "malware.exe";
            string contentType = "application/octet-stream";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert — exe files must be rejected
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_ForDocxFile()
        {
            // Arrange
            string fileName    = "contract.docx";
            string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_ForJpgFile()
        {
            // Arrange
            string fileName    = "photo.jpg";
            string contentType = "image/jpeg";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_WhenExtensionIsPdfButMimeTypeIsWrong()
        {
            // Arrange — attacker renames a .exe to .pdf but MIME type betrays it
            string fileName    = "disguised.pdf";
            string contentType = "application/octet-stream";  // wrong MIME type

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert — dual validation rejects this
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_WhenMimeTypeIsPdfButExtensionIsWrong()
        {
            // Arrange — correct MIME but wrong extension
            string fileName    = "agreement.txt";
            string contentType = "application/pdf";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_ForNullFileName()
        {
            // Arrange
            string? fileName   = null;
            string contentType = "application/pdf";

            // Act
            bool result = _service.IsValidPdf(fileName!, contentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_ReturnsFalse_ForEmptyFileName()
        {
            // Arrange
            string fileName    = "";
            string contentType = "application/pdf";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidPdf_IsCaseInsensitive_ForExtension()
        {
            // Arrange — uppercase extension should still be accepted
            string fileName    = "AGREEMENT.PDF";
            string contentType = "application/pdf";

            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("file.exe",  "application/octet-stream", false)]
        [InlineData("file.pdf",  "application/pdf",           true)]
        [InlineData("file.zip",  "application/zip",           false)]
        [InlineData("file.png",  "image/png",                 false)]
        [InlineData("file.js",   "text/javascript",           false)]
        [InlineData("file.bat",  "application/bat",           false)]
        [InlineData("file.PDF",  "application/pdf",           true)]  // case insensitive
        public void IsValidPdf_Parameterized_VariousFileTypes(
            string fileName, string contentType, bool expected)
        {
            // Act
            bool result = _service.IsValidPdf(fileName, contentType);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
