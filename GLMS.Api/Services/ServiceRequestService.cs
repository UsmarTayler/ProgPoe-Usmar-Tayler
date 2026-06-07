using GLMS.Api.Data;
using GLMS.Api.Patterns.Strategy;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The service implementation for ServiceRequest operations.
    ///
    /// WHAT I DID (Part 3 — Architectural Integrity):
    /// I moved the Strategy Pattern and the workflow business rule out of the
    /// ServiceRequestsController (API controller) into this service class.
    ///
    /// Business logic this service owns:
    ///   1. WORKFLOW RULE: Validates that the parent contract is Active or Draft
    ///      before creating a service request. If the contract is Expired or OnHold,
    ///      CreateAsync returns an error message — no exception is thrown.
    ///
    ///   2. STRATEGY PATTERN: On create, fetches the live USD→ZAR exchange rate,
    ///      then uses FinancialProcessor + UsdToZarStrategy to calculate CostZAR.
    ///
    /// The ServiceRequestsController now contains ZERO business logic —
    /// it only decides what HTTP status code to return based on the service result.
    /// </summary>
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyService     _currencyService;

        public ServiceRequestService(ApplicationDbContext context, ICurrencyService currencyService)
        {
            _context         = context;
            _currencyService = currencyService;
        }

        public async Task<List<ServiceRequest>> GetAllAsync(int? contractId)
        {
            var query = _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .AsQueryable();

            if (contractId.HasValue)
                query = query.Where(sr => sr.ContractId == contractId.Value);

            return await query.OrderByDescending(sr => sr.CreatedAt).ToListAsync();
        }

        public async Task<ServiceRequest?> GetByIdAsync(int id)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Contract)
                    .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        /// <summary>
        /// Creates a ServiceRequest, applying workflow validation + Strategy Pattern.
        /// Returns the created request, or an error message if the business rule is violated.
        /// </summary>
        public async Task<(ServiceRequest? Request, string? Error)> CreateAsync(ServiceRequest request)
        {
            // PRESENTATION POINT: WORKFLOW RULE
            // Business rule: ServiceRequests cannot be created on Expired or OnHold contracts.
            var contract = await _context.Contracts.FindAsync(request.ContractId);
            if (contract == null)
                return (null, "The selected contract does not exist.");

            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
                return (null, $"Cannot create a Service Request for a contract with status '{contract.Status}'. Only Active or Draft contracts are allowed.");

            // PRESENTATION POINT: STRATEGY PATTERN
            // Step 1 — Create the concrete strategy (USD→ZAR conversion)
            var strategy  = new UsdToZarStrategy();
            // Step 2 — Inject it into the context (FinancialProcessor)
            var processor = new FinancialProcessor(strategy);
            // Step 3 — Fetch the live rate (re-fetch server-side for security)
            var rate      = await _currencyService.GetUsdToZarRateAsync();

            request.ExchangeRateUsed = rate;
            // Step 4 — processor.Process() calls strategy.Convert() internally
            request.CostZAR          = processor.Process(request.CostUSD, rate);
            request.CreatedAt        = DateTime.UtcNow;

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return (request, null);
        }

        public async Task<bool> UpdateAsync(int id, ServiceRequest request)
        {
            if (!await _context.ServiceRequests.AnyAsync(sr => sr.Id == id))
                return false;

            request.Id = id;
            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.ServiceRequests.FindAsync(id);
            if (request == null) return false;
            _context.ServiceRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetExchangeRateAsync()
            => await _currencyService.GetUsdToZarRateAsync();
    }
}
