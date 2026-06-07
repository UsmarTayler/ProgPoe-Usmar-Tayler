namespace GLMS.Web.Patterns.Strategy
{
    /// <summary>
    /// STRATEGY PATTERN — Concrete Strategy #2: EUR → ZAR
    ///
    /// Encapsulates Euro-to-Rand conversion. Adding this strategy required
    /// zero changes to FinancialProcessor — demonstrating the power of the pattern.
    /// </summary>
    public class EurToZarStrategy : ICurrencyStrategy
    {
        public string SourceCurrency => "EUR";
        public string TargetCurrency => "ZAR";

        public decimal Convert(decimal amount, decimal rate)
        {
            if (rate <= 0)
                throw new ArgumentException("Exchange rate must be greater than zero.", nameof(rate));

            return Math.Round(amount * rate, 2);
        }
    }
}
