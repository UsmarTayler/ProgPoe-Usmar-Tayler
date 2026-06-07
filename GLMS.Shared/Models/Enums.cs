/// <summary>
/// Shared enumerations used by both GLMS.Api and GLMS.Web projects.
/// Centralising enums in the Shared class library ensures both projects
/// always reference the same set of valid values, preventing drift between
/// the API layer and the MVC client layer.
/// </summary>
namespace GLMS.Shared.Models
{
    /// <summary>
    /// Represents the lifecycle state of a Contract.
    /// </summary>
    public enum ContractStatus
    {
        Draft,
        Active,
        Expired,
        OnHold
    }

    /// <summary>
    /// Represents the tier of service agreed upon in a Contract.
    /// </summary>
    public enum ServiceLevel
    {
        Basic,
        Standard,
        Premium
    }

    /// <summary>
    /// Represents the current state of a ServiceRequest.
    /// </summary>
    public enum ServiceRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Distinguishes between domestic and cross-border contracts.
    /// Drives the Factory Method Pattern: Local uses LocalContractFactory,
    /// International uses InternationalContractFactory.
    /// </summary>
    public enum ContractType
    {
        Local,
        International
    }
}
