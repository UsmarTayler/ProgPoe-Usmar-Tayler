/// <summary>
/// WHAT THIS FILE IS:
/// Defines all the enumerations (enums) used throughout the GLMS application.
///
/// WHAT I DID:
/// I created all the domain-specific enums here in one central file so they are
/// easy to find and explain. Enums are used instead of "magic strings" or integers
/// because they make the code self-documenting and prevent invalid values.
///
/// EF CORE STORAGE:
/// In ApplicationDbContext I configured all enums to store as strings
/// (e.g., "Active", "Draft") rather than integers (0, 1). This makes the
/// database readable without needing the code to understand what 0 or 1 means.
/// </summary>
namespace GLMS.Web.Models
{
    /// <summary>
    /// Represents the lifecycle status of a Contract.
    /// The Observer Pattern fires whenever this status changes.
    /// </summary>
    public enum ContractStatus
    {
        Draft,
        Active,
        Expired,
        OnHold
    }

    /// <summary>
    /// Defines the tier of service agreed upon in a Contract.
    /// </summary>
    public enum ServiceLevel
    {
        Basic,
        Standard,
        Premium
    }

    /// <summary>
    /// Represents the processing state of a ServiceRequest.
    /// </summary>
    public enum ServiceRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Determines which ContractFactory to use (Factory Method Pattern).
    /// </summary>
    public enum ContractType
    {
        Local,
        International
    }
}
