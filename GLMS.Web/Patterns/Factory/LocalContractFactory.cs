using GLMS.Web.Models;

namespace GLMS.Web.Patterns.Factory
{
    /// <summary>
    /// FACTORY METHOD PATTERN — Concrete Factory #1
    ///
    /// Creates a LOCAL contract. Local contracts operate within a single region
    /// and default to Draft status. The factory encapsulates all the creation
    /// logic so the ContractsController never has to know which type it is building.
    /// </summary>
    public class LocalContractFactory : IContractFactory
    {
        public Contract CreateContract(int clientId, DateTime startDate, DateTime endDate, ServiceLevel serviceLevel)
        {
            return new Contract
            {
                ClientId     = clientId,
                StartDate    = startDate,
                EndDate      = endDate,
                ServiceLevel = serviceLevel,
                ContractType = ContractType.Local,
                Status       = ContractStatus.Draft   // All new contracts start as Draft
            };
        }

        public string GetContractTypeDescription() =>
            "Local Contract — operates within a single geographic region.";
    }
}
