using GLMS.Shared.Models;

namespace GLMS.Api.Services
{
    /// <summary>
    /// Service interface for Contract CRUD operations.
    /// Keeps the ContractsController thin — it only handles HTTP concerns.
    /// All Factory Method Pattern, Observer Pattern, file handling, and DB access
    /// lives inside ContractService, not in the controller.
    /// </summary>
    public interface IContractService
    {
        Task<List<Contract>> GetAllAsync(string? statusFilter, string? startFrom, string? startTo);
        Task<Contract?> GetByIdAsync(int id);
        Task<(Contract? Contract, string? Error)> CreateAsync(
            int clientId, DateTime startDate, DateTime endDate,
            string serviceLevel, string contractType, IFormFile? signedAgreement, string uploadsPath);
        Task<(bool Success, string? Error)> UpdateAsync(
            int id, Contract contract, IFormFile? signedAgreement, string uploadsPath);
        Task<(bool Success, string? Error)> PatchStatusAsync(int id, string status);
        Task<bool> DeleteAsync(int id);
        Task<(string? FilePath, string? FileName)> GetAgreementFileAsync(int id);
    }
}
