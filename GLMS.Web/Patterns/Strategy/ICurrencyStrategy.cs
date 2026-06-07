namespace GLMS.Web.Patterns.Strategy
{
    /// <summary>
    /// STRATEGY PATTERN — Interface (from Part 1 UML: ICurrencyStrategy)
    ///
    /// Defines the algorithm contract for currency conversion.
    /// Each concrete strategy encapsulates a specific conversion logic
    /// (e.g., USD→ZAR, EUR→ZAR). The FinancialProcessor uses this interface
    /// so it can swap strategies at runtime without changing its own code.
    ///
    /// Benefit: If TechMove adds a new currency provider, we add a new Strategy
    /// class — zero changes to the FinancialProcessor or controllers.
    /// </summary>
    public interface ICurrencyStrategy
    {
        string SourceCurrency { get; }
        string TargetCurrency { get; }

        /// <summary>
        /// Converts an amount from SourceCurrency to TargetCurrency using the given rate.
        /// </summary>
        /// <param name="amount">The amount in source currency.</param>
        /// <param name="rate">The current exchange rate (source → target).</param>
        /// <returns>The converted amount in target currency.</returns>
        decimal Convert(decimal amount, decimal rate);
    }
}
