using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The Client model represents a TechMove Logistics customer stored in the database.
    /// It is a plain C# class (POCO) decorated with Data Annotations for both
    /// validation and EF Core schema generation.
    ///
    /// WHAT I DID:
    /// I used Data Annotations ([Required], [StringLength], [EmailAddress], [Phone])
    /// directly on the properties so that ASP.NET Core MVC validates the form
    /// BEFORE the data ever reaches the database. If validation fails,
    /// ModelState.IsValid returns false and the controller shows the form again
    /// with error messages — no DB call is made.
    ///
    /// RELATIONSHIP:
    /// One Client → Many Contracts (one-to-many).
    /// The ICollection<Contract> navigation property lets EF Core join the tables
    /// automatically when we call .Include(c => c.Contracts).
    /// </summary>
    public class Client
    {
        // Primary key — EF Core detects "Id" by convention and makes it the PK + auto-increment
        public int Id { get; set; }

        // [Required] — form cannot be submitted blank
        // [StringLength] — enforced in both the UI and the database column (nvarchar(150))
        // [Display] — changes the label shown in Razor views from "Name" to "Full Name"
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        // [EmailAddress] — validates the format (must contain '@' and a domain)
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        // [Phone] — validates basic phone number format
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required.")]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        // NAVIGATION PROPERTY — EF Core uses this to JOIN the Clients and Contracts tables.
        // When we write: _context.Clients.Include(c => c.Contracts)
        // EF Core runs: SELECT * FROM Clients LEFT JOIN Contracts ON Contracts.ClientId = Clients.Id
        // Initialised as an empty list so it's never null (avoids NullReferenceException).
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
