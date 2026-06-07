using GLMS.Shared.Models;

namespace GLMS.Api.Services
{
    /// <summary>
    /// Service interface for ServiceRequest operations.
    /// Strategy Pattern (USD→ZAR) and workflow business rules live in the
    /// implementation — not in the controller.
    /// </summary>
    public interface IServiceRequestService
    {
        Task<List<ServiceRequest>> GetAllAsync(int? contractId);
        Task<ServiceRequest?> GetByIdAsync(int id);
        Task<(ServiceRequest? Request, string? Error)> CreateAsync(ServiceRequest request);
        Task<bool> UpdateAsync(int id, ServiceRequest request);
        Task<bool> DeleteAsync(int id);
        Task<decimal> GetExchangeRateAsync();
    }
}
