using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The ServiceRequest model — represents a billable task raised under a Contract.
    ///
    /// WHAT I DID:
    /// I designed this model to connect with the Strategy Pattern:
    ///   - The user enters a cost in USD on the Create form.
    ///   - The ServiceRequestsController fetches a live exchange rate (async/await, LU4).
    ///   - The FinancialProcessor (Strategy Pattern context) calls UsdToZarStrategy.Convert()
    ///     to calculate the ZAR equivalent.
    ///   - Both CostUSD and CostZAR are stored in the database so future reports can
    ///     show historical amounts even if the exchange rate changes.
    ///
    /// WORKFLOW RULE (from the brief):
    ///   A ServiceRequest can ONLY be created if the parent Contract is Active or Draft.
    ///   If the contract is Expired or OnHold, the controller adds a ModelState error
    ///   and the form is rejected. The ServiceRequestObserver also retroactively cancels
    ///   Pending requests when a contract's status changes.
    /// </summary>
    public class ServiceRequest
    {
        public int Id { get; set; }

        // Foreign key to Contract
        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        // Navigation property
        public Contract? Contract { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The cost entered by the user in USD.
        /// </summary>
        [Required(ErrorMessage = "USD cost is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than zero.")]
        [Display(Name = "Cost (USD)")]
        [DataType(DataType.Currency)]
        public decimal CostUSD { get; set; }

        /// <summary>
        /// Auto-calculated ZAR cost using the Strategy Pattern (FinancialProcessor).
        /// Saved to DB so it reflects the exchange rate at time of creation.
        /// </summary>
        [Display(Name = "Cost (ZAR)")]
        [DataType(DataType.Currency)]
        public decimal CostZAR { get; set; }

        /// <summary>
        /// The USD-to-ZAR exchange rate fetched from the external API at time of creation.
        /// </summary>
        [Display(Name = "Exchange Rate (USD→ZAR)")]
        public decimal ExchangeRateUsed { get; set; }

        [Required]
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
