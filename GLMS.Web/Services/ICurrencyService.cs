namespace GLMS.Web.Services
{
    /// <summary>
    /// Defines the contract for fetching live exchange rates from an external API.
    /// Abstracted as an interface so the implementation can be swapped or mocked in tests.
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// Fetches the current USD-to-ZAR exchange rate from the external API.
        /// </summary>
        /// <returns>The exchange rate (e.g., 18.75 means $1 = R18.75)</returns>
        Task<decimal> GetUsdToZarRateAsync();
    }
}
