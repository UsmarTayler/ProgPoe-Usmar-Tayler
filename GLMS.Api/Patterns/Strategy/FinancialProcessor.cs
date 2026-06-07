namespace GLMS.Api.Patterns.Strategy
{
    /// <summary>
    /// STRATEGY PATTERN — Context (from Part 1 UML: FinancialProcessor)
    ///
    /// The FinancialProcessor holds a reference to an ICurrencyStrategy and
    /// delegates all conversion work to it. The controller injects the correct
    /// strategy at runtime based on the contract type.
    ///
    /// This means the FinancialProcessor can switch between USD→ZAR and EUR→ZAR
    /// (or any future currency) at runtime without any code changes here.
    /// This is the core "open for extension, closed for modification" principle.
    /// </summary>
    public class FinancialProcessor
    {
        // PRESENTATION POINT: This private field holds whichever strategy is currently active.
        // The FinancialProcessor doesn't care whether it's USD→ZAR or EUR→ZAR.
        // It just knows it has "some strategy" that can Convert().
        private ICurrencyStrategy _strategy;

        // Constructor Injection: the strategy is passed in from outside.
        // In the ServiceRequestsController we write:
        //   var processor = new FinancialProcessor(new UsdToZarStrategy());
        public FinancialProcessor(ICurrencyStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Swaps the active conversion strategy at runtime.
        /// Example: switch from UsdToZar to EurToZar without recreating the processor.
        /// </summary>
        public void SetStrategy(ICurrencyStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>
        /// Processes the currency conversion using the active strategy.
        /// </summary>
        /// <param name="amount">Amount in source currency.</param>
        /// <param name="rate">Current exchange rate.</param>
        /// <returns>Converted amount in ZAR.</returns>
        public decimal Process(decimal amount, decimal rate)
        {
            // PRESENTATION POINT: This single line is the heart of the Strategy Pattern.
            // We call Convert() on the strategy — we don't know and don't NEED to know
            // which strategy it is. If tomorrow TechMove adds GBP→ZAR, this line stays
            // exactly the same. Only a new Strategy class needs to be added.
            return _strategy.Convert(amount, rate);
        }

        public string GetConversionDescription() =>
            $"{_strategy.SourceCurrency} → {_strategy.TargetCurrency}";
    }
}
