using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The Contract model — the central entity of the GLMS application.
    /// Every contract belongs to a Client and can have many ServiceRequests.
    ///
    /// WHAT I DID:
    /// I designed this model to connect two of the three design patterns:
    ///   - FACTORY METHOD: Contracts are never created with "new Contract()" directly.
    ///     Instead the controller calls ContractFactoryResolver.GetFactory(type).CreateContract(...)
    ///     which sets ContractType and Status automatically.
    ///   - OBSERVER: The Status property is monitored. When it changes in the Edit form,
    ///     ContractStatusSubject.NotifyAsync() is called, which triggers all observers.
    ///
    /// PROPERTIES:
    ///   SignedAgreementPath — the server file path for the uploaded PDF (stored in /wwwroot/uploads/)
    ///   SignedAgreementOriginalName — the original filename displayed to the user in the UI
    ///   ServiceRequests — navigation property for eager loading with .Include()
    /// </summary>
    public class Contract
    {
        public int Id { get; set; }

        // Foreign key to Client
        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        // Navigation property
        public Client? Client { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        [Display(Name = "Service Level")]
        public ServiceLevel ServiceLevel { get; set; } = ServiceLevel.Standard;

        [Required]
        [Display(Name = "Contract Type")]
        public ContractType ContractType { get; set; } = ContractType.Local;

        /// <summary>
        /// Relative path to the uploaded PDF "Signed Agreement" on the file server.
        /// Null if no file has been uploaded yet.
        /// </summary>
        [Display(Name = "Signed Agreement (PDF)")]
        public string? SignedAgreementPath { get; set; }

        [Display(Name = "Original File Name")]
        public string? SignedAgreementOriginalName { get; set; }

        // Navigation property — a contract has many service requests
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
