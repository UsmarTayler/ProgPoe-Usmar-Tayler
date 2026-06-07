namespace GLMS.Api.Services
{
    /// <summary>Dashboard stats DTO returned by the dashboard service.</summary>
    public record DashboardStatsDto(
        int     TotalClients,
        int     TotalContracts,
        int     ActiveContracts,
        int     TotalServiceRequests,
        int     PendingRequests,
        decimal TotalRevenue);

    /// <summary>Service interface for dashboard statistics.</summary>
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
    }
}
