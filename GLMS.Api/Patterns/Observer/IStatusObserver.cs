using GLMS.Shared.Models;

namespace GLMS.Api.Patterns.Observer
{
    /// <summary>
    /// OBSERVER PATTERN — Observer Interface (from Part 1 UML: IStatusObserver)
    ///
    /// Any module that needs to react to a contract status change must implement
    /// this interface. Current observers:
    ///   - ServiceRequestObserver: Cancels pending requests when contract expires/goes on hold.
    ///   - FinanceObserver: Logs a financial alert when contract status changes.
    ///
    /// Adding a new observer (e.g., EmailNotificationObserver) requires only:
    ///   1. Create a new class that implements IStatusObserver.
    ///   2. Register it with ContractStatusSubject.
    ///   Zero changes to existing code.
    /// </summary>
    public interface IStatusObserver
    {
        /// <summary>
        /// Called by the Subject when the contract's status changes.
        /// </summary>
        /// <param name="contractId">The ID of the contract whose status changed.</param>
        /// <param name="newStatus">The new status value.</param>
        Task UpdateAsync(int contractId, ContractStatus newStatus);
    }
}
