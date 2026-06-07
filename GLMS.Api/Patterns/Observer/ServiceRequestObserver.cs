using GLMS.Api.Data;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Patterns.Observer
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// Concrete Observer #1 in the Observer Pattern (from Part 1 UML).
    ///
    /// WHAT I DID:
    /// This class implements IStatusObserver and reacts to contract status changes.
    /// When a contract changes to Expired or OnHold, this observer automatically
    /// cancels all Pending ServiceRequests belonging to that contract.
    ///
    /// WHY THIS MATTERS (Workflow Rule from brief):
    ///   "A ServiceRequest cannot be created if the parent Contract is Expired or OnHold."
    /// This observer enforces that rule RETROACTIVELY — existing pending requests
    /// are cancelled the moment the contract status changes.
    ///
    /// HOW IT CONNECTS:
    /// The ContractStatusSubject calls UpdateAsync() on every registered observer.
    /// This observer is registered (Attached) in Program.cs at startup.
    /// The ContractsController triggers it with a single line:
    ///   await _statusSubject.NotifyAsync(contract.Id, contract.Status);
    /// </summary>
    public class ServiceRequestObserver : IStatusObserver
    {
        // ApplicationDbContext injected so we can query and update ServiceRequests
        private readonly ApplicationDbContext _context;

        public ServiceRequestObserver(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateAsync(int contractId, ContractStatus newStatus)
        {
            // PRESENTATION POINT: We only act when the contract becomes "restricted".
            // If the status changes to Active or Draft, no cancellation is needed.
            if (newStatus == ContractStatus.Expired || newStatus == ContractStatus.OnHold)
            {
                // LINQ query: find all Pending requests for this specific contract
                // SQL equivalent: SELECT * FROM ServiceRequests
                //                 WHERE ContractId = @contractId AND Status = 'Pending'
                var pendingRequests = await _context.ServiceRequests
                    .Where(sr => sr.ContractId == contractId
                              && sr.Status == ServiceRequestStatus.Pending)
                    .ToListAsync();

                // Loop through each and mark as Cancelled
                foreach (var request in pendingRequests)
                {
                    request.Status = ServiceRequestStatus.Cancelled;
                }

                // Only call SaveChangesAsync if there was actually something to cancel
                // (avoids an unnecessary roundtrip to the database)
                if (pendingRequests.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
