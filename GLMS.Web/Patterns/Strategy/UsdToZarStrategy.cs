namespace GLMS.Web.Patterns.Strategy
{
    /// <summary>
    /// STRATEGY PATTERN — Concrete Strategy #1: USD → ZAR
    ///
    /// Encapsulates the logic to convert US Dollars to South African Rand.
    /// This is the primary strategy used on the ServiceRequest creation page,
    /// where the live rate is fetched from the external ExchangeRate API.
    /// </summary>
    public class UsdToZarStrategy : ICurrencyStrategy
    {
        public string SourceCurrency => "USD";
        public string TargetCurrency => "ZAR";

        /// <summary>
        /// Multiplies the USD amount by the current USD-to-ZAR rate.
        /// Example: $100 × 18.75 = R1,875.00
        /// </summary>
        public decimal Convert(decimal amount, decimal rate)
        {
            // PRESENTATION POINT: This is the actual conversion formula.
            // We validate the rate first — a rate of 0 would cause division-by-zero
            // or nonsensical results (R0 for any amount), so we throw early.
            if (rate <= 0)
                throw new ArgumentException("Exchange rate must be greater than zero.", nameof(rate));

            // The core calculation: multiply amount × rate, then round to 2 decimal places
            // (because you can't have fractions of a cent in ZAR).
            // Example: $100.00 × 18.75 = R1,875.00
            return Math.Round(amount * rate, 2);
        }
    }
}
