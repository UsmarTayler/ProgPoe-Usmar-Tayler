using GLMS.Web.Models;

namespace GLMS.Web.Patterns.Observer
{
    /// <summary>
    /// OBSERVER PATTERN — Concrete Observer #2
    ///
    /// The Finance module observer reacts to contract status changes by
    /// logging an audit trail entry. In a production system this would
    /// write to a FinanceAuditLog table — here it logs to the console
    /// as a demonstration of the pattern's extensibility.
    ///
    /// This was added to show that the Observer Pattern allows unlimited
    /// new modules to subscribe without any change to the Subject or
    /// other observers.
    /// </summary>
    public class FinanceObserver : IStatusObserver
    {
        private readonly ILogger<FinanceObserver> _logger;

        public FinanceObserver(ILogger<FinanceObserver> logger)
        {
            _logger = logger;
        }

        public Task UpdateAsync(int contractId, ContractStatus newStatus)
        {
            // In production: write to FinanceAuditLog table
            _logger.LogInformation(
                "[FinanceObserver] Contract {ContractId} status changed to {Status}. " +
                "Finance module has been notified at {Time}.",
                contractId, newStatus, DateTime.UtcNow);

            return Task.CompletedTask;
        }
    }
}
