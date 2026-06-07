using GLMS.Api.Data;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The service implementation for dashboard statistics.
    /// Runs all five aggregate queries against the database.
    ///
    /// WHAT I DID (Part 3):
    /// Previously these CountAsync queries lived directly in DashboardController.
    /// Moving them here means the controller action is a single line:
    ///   var stats = await _dashboardService.GetStatsAsync();
    ///   return Ok(stats);
    ///
    /// This is the ideal controller — it makes ONE service call and returns the result.
    /// All database knowledge is hidden behind IDashboardService.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Runs six aggregate queries against the database sequentially.
        ///
        /// PRESENTATION POINT — Async/Await (LU4):
        /// Each 'await' releases the thread while the database round-trip is in flight,
        /// so the web server is never blocked. Six sequential async calls are still
        /// far better than six synchronous calls — and EF Core requires sequential
        /// execution because a single DbContext is not thread-safe; running multiple
        /// queries in parallel on the same context throws InvalidOperationException.
        /// </summary>
        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var totalClients    = await _context.Clients.CountAsync();
            var totalContracts  = await _context.Contracts.CountAsync();
            var activeContracts = await _context.Contracts
                                        .CountAsync(c => c.Status == ContractStatus.Active);
            var totalRequests   = await _context.ServiceRequests.CountAsync();
            var pendingRequests = await _context.ServiceRequests
                                        .CountAsync(sr => sr.Status == ServiceRequestStatus.Pending);

            // SumAsync returns null for an empty table — coalesce to 0 after awaiting.
            var totalRevenue = (await _context.ServiceRequests
                                      .SumAsync(sr => (decimal?)sr.CostZAR)) ?? 0m;

            return new DashboardStatsDto(
                TotalClients:         totalClients,
                TotalContracts:       totalContracts,
                ActiveContracts:      activeContracts,
                TotalServiceRequests: totalRequests,
                PendingRequests:      pendingRequests,
                TotalRevenue:         totalRevenue
            );
        }
    }
}
