// Part 3: Updated to use GLMS.Api namespace (Patterns moved from GLMS.Web to GLMS.Api in the SOA refactor)
using GLMS.Api.Patterns.Strategy;
using Xunit;

namespace GLMS.Tests
{
    /// <summary>
    /// UNIT TESTS — Currency Calculation (xUnit)
    ///
    /// Tests the Strategy Pattern (FinancialProcessor + UsdToZarStrategy)
    /// in isolation — no database, no HTTP calls needed.
    ///
    /// Requirement from brief:
    ///   "Currency Calculation: Verify that the math converting USD to ZAR
    ///    is correct, given a specific rate."
    /// </summary>
    public class CurrencyCalculationTests
    {
        // =====================================================================
        // Tests for UsdToZarStrategy
        // =====================================================================

        [Fact]
        public void UsdToZarStrategy_ConvertsCorrectly_WithKnownRate()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();
            decimal amount = 100m;
            decimal rate   = 18.75m;

            // Act
            decimal result = strategy.Convert(amount, rate);

            // Assert
            Assert.Equal(1875.00m, result);
        }

        [Fact]
        public void UsdToZarStrategy_ConvertsCorrectly_WithDecimalAmount()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();
            decimal amount = 49.99m;
            decimal rate   = 18.50m;

            // Act
            decimal result = strategy.Convert(amount, rate);

            // Assert
            // 49.99 × 18.50 = 924.815 → rounded to 924.82
            Assert.Equal(924.82m, result);
        }

        [Fact]
        public void UsdToZarStrategy_ReturnZero_WhenAmountIsZero()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();

            // Act
            decimal result = strategy.Convert(0m, 18.75m);

            // Assert
            Assert.Equal(0m, result);
        }

        [Fact]
        public void UsdToZarStrategy_ThrowsArgumentException_WhenRateIsZero()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();

            // Act & Assert — rate of 0 is invalid
            Assert.Throws<ArgumentException>(() => strategy.Convert(100m, 0m));
        }

        [Fact]
        public void UsdToZarStrategy_ThrowsArgumentException_WhenRateIsNegative()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => strategy.Convert(100m, -5m));
        }

        [Fact]
        public void UsdToZarStrategy_HasCorrectCurrencyCodes()
        {
            // Arrange
            var strategy = new UsdToZarStrategy();

            // Assert
            Assert.Equal("USD", strategy.SourceCurrency);
            Assert.Equal("ZAR", strategy.TargetCurrency);
        }

        // =====================================================================
        // Tests for FinancialProcessor (Strategy Pattern Context)
        // =====================================================================

        [Fact]
        public void FinancialProcessor_ProcessesConversion_UsingUsdToZarStrategy()
        {
            // Arrange
            var strategy  = new UsdToZarStrategy();
            var processor = new FinancialProcessor(strategy);

            // Act
            decimal result = processor.Process(200m, 19.00m);

            // Assert
            // 200 × 19 = 3800
            Assert.Equal(3800.00m, result);
        }

        [Fact]
        public void FinancialProcessor_CanSwapStrategy_AtRuntime()
        {
            // Arrange — start with USD strategy
            var usdStrategy = new UsdToZarStrategy();
            var processor   = new FinancialProcessor(usdStrategy);

            decimal usdResult = processor.Process(100m, 18m);
            Assert.Equal(1800m, usdResult);

            // Act — swap to EUR strategy (demonstrates Strategy Pattern flexibility)
            var eurStrategy = new EurToZarStrategy();
            processor.SetStrategy(eurStrategy);

            decimal eurResult = processor.Process(100m, 20m);

            // Assert — EUR rate gives different result
            Assert.Equal(2000m, eurResult);
        }

        [Fact]
        public void FinancialProcessor_RoundsResult_ToTwoDecimalPlaces()
        {
            // Arrange
            var processor = new FinancialProcessor(new UsdToZarStrategy());

            // Act — 33.33 × 3 = 99.99 (exact), 10 × 3.333 = 33.33
            decimal result = processor.Process(10m, 3.333m);

            // Assert — must be rounded to 2 decimal places
            Assert.Equal(Math.Round(10m * 3.333m, 2), result);
        }

        [Theory]
        [InlineData(50,   18.00, 900.00)]
        [InlineData(1000, 19.50, 19500.00)]
        [InlineData(0.01, 18.75, 0.19)]   // very small amount
        [InlineData(1,    1.00,  1.00)]   // 1:1 rate edge case
        public void UsdToZarStrategy_ConvertsCorrectly_Parameterized(
            decimal amount, decimal rate, decimal expected)
        {
            // Arrange
            var strategy = new UsdToZarStrategy();

            // Act
            decimal result = strategy.Convert(amount, rate);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
