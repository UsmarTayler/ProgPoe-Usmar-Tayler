using GLMS.Shared.Models;

namespace GLMS.Api.Patterns.Factory
{
    /// <summary>
    /// FACTORY METHOD PATTERN — Resolver / Registry
    ///
    /// Acts as a simple factory registry. Given a ContractType enum value,
    /// it returns the correct concrete IContractFactory.
    ///
    /// This keeps the controller clean — it just calls:
    ///   var factory = ContractFactoryResolver.GetFactory(contractType);
    ///   var contract = factory.CreateContract(...);
    /// </summary>
    public static class ContractFactoryResolver
    {
        public static IContractFactory GetFactory(ContractType contractType)
        {
            return contractType switch
            {
                ContractType.Local         => new LocalContractFactory(),
                ContractType.International => new InternationalContractFactory(),
                _                          => new LocalContractFactory()
            };
        }
    }
}
