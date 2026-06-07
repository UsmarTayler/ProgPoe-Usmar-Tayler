using GLMS.Shared.Models;

namespace GLMS.Api.Patterns.Factory
{
    /// <summary>
    /// FACTORY METHOD PATTERN — Concrete Factory #2
    ///
    /// Creates an INTERNATIONAL contract. International contracts span multiple
    /// regions and currencies — which is why the Strategy Pattern for currency
    /// conversion is especially relevant for ServiceRequests under these contracts.
    /// </summary>
    public class InternationalContractFactory : IContractFactory
    {
        public Contract CreateContract(int clientId, DateTime startDate, DateTime endDate, ServiceLevel serviceLevel)
        {
            return new Contract
            {
                ClientId     = clientId,
                StartDate    = startDate,
                EndDate      = endDate,
                ServiceLevel = serviceLevel,
                ContractType = ContractType.International,
                Status       = ContractStatus.Draft
            };
        }

        public string GetContractTypeDescription() =>
            "International Contract — spans multiple regions and currencies.";
    }
}
