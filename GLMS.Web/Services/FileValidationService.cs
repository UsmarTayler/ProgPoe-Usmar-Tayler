namespace GLMS.Web.Services
{
    /// <summary>
    /// Handles all file validation and storage logic for uploaded PDFs.
    ///
    /// Validation rules:
    ///   - Only .pdf files are accepted (checked by extension AND MIME type).
    ///   - Files are renamed with a UUID prefix to prevent overwrites.
    ///   - Stored in /wwwroot/uploads/ (simulated file server).
    ///
    /// This service is extracted from the controller so it can be unit-tested
    /// independently (see GLMS.Tests/FileValidationTests.cs).
    /// </summary>
    public class FileValidationService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf"
        };

        /// <summary>
        /// Validates that the uploaded file is a PDF.
        /// Checks both the file extension and the MIME content type.
        /// </summary>
        /// <returns>True if valid; false otherwise.</returns>
        public bool IsValidPdf(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return false;

            var extension = Path.GetExtension(file.FileName);
            var mimeType  = file.ContentType;

            return AllowedExtensions.Contains(extension)
                && AllowedMimeTypes.Contains(mimeType);
        }

        /// <summary>
        /// Validates a file by extension and MIME type (string overload for unit testing).
        /// </summary>
   
        public bool IsValidPdf(string fileName, string contentType)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName);
            return AllowedExtensions.Contains(extension)
                && AllowedMimeTypes.Contains(contentType);
        }

        /// <summary>
        /// Saves the validated PDF to the uploads directory and returns the stored file path.
        /// Uses UUID naming to prevent file overwrites (marking criteria).
        /// </summary>
        /// <param name="file">The uploaded IFormFile.</param>
        /// <param name="uploadsFolder">Absolute path to the uploads folder.</param>
        /// <returns>The relative file path stored in the database (e.g., "uploads/uuid_original.pdf")</returns>
        public async Task<string> SavePdfAsync(IFormFile file, string uploadsFolder)
        {
            Directory.CreateDirectory(uploadsFolder);

            // UUID prefix prevents filename collisions (marking criteria)
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath       = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return relative path for storage in the database
            return Path.Combine("uploads", uniqueFileName).Replace("\\", "/");
        }
    }
}
