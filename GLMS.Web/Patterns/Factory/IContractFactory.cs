using GLMS.Web.Models;

namespace GLMS.Web.Patterns.Factory
{
    /// <summary>
    /// FACTORY METHOD PATTERN — Interface (from Part 1 UML)
    ///
    /// Defines the contract for creating Contract objects.
    /// Each concrete factory (Local, International) implements this interface
    /// and sets the appropriate ContractType, allowing new contract types to be
    /// added in the future without modifying existing code (Open/Closed Principle).
    ///
    /// Mapped from Part 1: IContractFactory interface in the UML diagram.
    /// </summary>
    public interface IContractFactory
    {
        /// <summary>
        /// Creates and returns a new Contract with the factory's default settings applied.
        /// </summary>
        Contract CreateContract(int clientId, DateTime startDate, DateTime endDate, ServiceLevel serviceLevel);

        /// <summary>
        /// Returns a human-readable description of this factory's contract type.
        /// </summary>
        string GetContractTypeDescription();
    }
}
