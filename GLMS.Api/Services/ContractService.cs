using GLMS.Api.Data;
using GLMS.Api.Patterns.Factory;
using GLMS.Api.Patterns.Observer;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The service implementation for Contract operations.
    ///
    /// WHAT I DID (Part 3 — Architectural Integrity):
    /// This is the most important service class in the project because it brings together
    /// THREE design patterns that previously lived in the MVC controller:
    ///
    ///   1. FACTORY METHOD PATTERN — CreateAsync() calls ContractFactoryResolver
    ///      to create the right contract type (Local or International).
    ///
    ///   2. OBSERVER PATTERN — UpdateAsync() and PatchStatusAsync() detect
    ///      status changes and call ContractStatusSubject.NotifyAsync() to fire
    ///      all registered observers (ServiceRequestObserver, FinanceObserver).
    ///
    ///   3. FILE HANDLING (PDF validation + storage) — CreateAsync() and UpdateAsync()
    ///      delegate to FileValidationService for PDF verification and UUID naming.
    ///
    /// The ContractsController (API endpoint layer) now contains ZERO business logic.
    /// It just calls these service methods and maps the results to HTTP responses.
    ///
    /// PRESENTATION POINT — why this matters:
    /// If we ever wanted to create a contract from a background job (not an HTTP request),
    /// we'd just call ContractService.CreateAsync() directly — no controller needed.
    /// That's the power of separating the service layer from the HTTP layer.
    /// </summary>
    public class ContractService : IContractService
    {
        private readonly ApplicationDbContext  _context;
        private readonly FileValidationService _fileService;
        private readonly ContractStatusSubject _statusSubject;

        public ContractService(
            ApplicationDbContext context,
            FileValidationService fileService,
            ContractStatusSubject statusSubject)
        {
            _context       = context;
            _fileService   = fileService;
            _statusSubject = statusSubject;
        }

        /// <summary>
        /// Returns all contracts with Client included.
        /// Supports optional filtering by status and start-date range.
        /// </summary>
        public async Task<List<Contract>> GetAllAsync(string? statusFilter, string? startFrom, string? startTo)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<ContractStatus>(statusFilter, out var parsedStatus))
                query = query.Where(c => c.Status == parsedStatus);

            if (DateTime.TryParse(startFrom, out var from))
                query = query.Where(c => c.StartDate >= from);

            if (DateTime.TryParse(startTo, out var to))
                query = query.Where(c => c.StartDate <= to);

            return await query.OrderByDescending(c => c.StartDate).ToListAsync();
        }

        /// <summary>Returns a single contract with Client and ServiceRequests, or null.</summary>
        public async Task<Contract?> GetByIdAsync(int id)
        {
            return await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Creates a contract using the Factory Method Pattern.
        /// Validates and saves the PDF file if provided.
        /// </summary>
        public async Task<(Contract? Contract, string? Error)> CreateAsync(
            int clientId, DateTime startDate, DateTime endDate,
            string serviceLevel, string contractType,
            IFormFile? signedAgreement, string uploadsPath)
        {
            // Validate inputs
            if (!Enum.TryParse<ServiceLevel>(serviceLevel, out var sl))
                return (null, $"Invalid service level: {serviceLevel}");
            if (!Enum.TryParse<ContractType>(contractType, out var ct))
                return (null, $"Invalid contract type: {contractType}");

            // PRESENTATION POINT: FACTORY METHOD PATTERN
            // The resolver picks the right factory based on contractType.
            // New contract types can be added by creating a new factory — no changes here.
            var factory  = ContractFactoryResolver.GetFactory(ct);
            var contract = factory.CreateContract(clientId, startDate, endDate, sl);

            // Validate and save PDF if provided
            if (signedAgreement != null)
            {
                if (!_fileService.IsValidPdf(signedAgreement))
                    return (null, "Only PDF files are allowed for the Signed Agreement.");

                contract.SignedAgreementPath         = await _fileService.SavePdfAsync(signedAgreement, uploadsPath);
                contract.SignedAgreementOriginalName = signedAgreement.FileName;
            }

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return (contract, null);
        }

        /// <summary>
        /// Updates a contract.
        /// If status changed, fires the Observer pattern (notifies all observers).
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateAsync(
            int id, Contract contract, IFormFile? signedAgreement, string uploadsPath)
        {
            var existing = await _context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return (false, "Contract not found.");

            // Keep existing file if no new file was uploaded
            if (signedAgreement != null)
            {
                if (!_fileService.IsValidPdf(signedAgreement))
                    return (false, "Only PDF files are allowed for the Signed Agreement.");

                contract.SignedAgreementPath         = await _fileService.SavePdfAsync(signedAgreement, uploadsPath);
                contract.SignedAgreementOriginalName = signedAgreement.FileName;
            }
            else
            {
                contract.SignedAgreementPath         = existing.SignedAgreementPath;
                contract.SignedAgreementOriginalName = existing.SignedAgreementOriginalName;
            }

            contract.Id = id;
            _context.Entry(contract).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // PRESENTATION POINT: OBSERVER PATTERN trigger
            // Compare old status vs new status. Only notify if it actually changed.
            if (existing.Status != contract.Status)
                await _statusSubject.NotifyAsync(contract.Id, contract.Status);

            return (true, null);
        }

        /// <summary>
        /// Patches just the Status field of a contract (PATCH /api/contracts/{id}/status).
        /// Fires Observer pattern if status changed.
        /// </summary>
        public async Task<(bool Success, string? Error)> PatchStatusAsync(int id, string status)
        {
            if (!Enum.TryParse<ContractStatus>(status, out var newStatus))
                return (false, $"Invalid status value: '{status}'. Valid values: Draft, Active, Expired, OnHold.");

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return (false, "Contract not found.");

            var oldStatus = contract.Status;
            contract.Status = newStatus;
            await _context.SaveChangesAsync();

            // OBSERVER PATTERN: fire if status changed
            if (oldStatus != newStatus)
                await _statusSubject.NotifyAsync(id, newStatus);

            return (true, null);
        }

        /// <summary>Deletes a contract by ID. Returns false if not found.</summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null) return false;

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Returns the file path and original name for the signed agreement PDF.</summary>
        public async Task<(string? FilePath, string? FileName)> GetAgreementFileAsync(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            return (contract?.SignedAgreementPath, contract?.SignedAgreementOriginalName);
        }
    }
}
