using System.Text.Json;

namespace GLMS.Web.Services
{
    /// <summary>
    /// Fetches the live USD-to-ZAR exchange rate from the free ExchangeRate-API.
    /// Endpoint: https://open.er-api.com/v6/latest/USD  (no API key required)
    ///
    /// Uses Async/Await with HttpClient (LU4: Optimising Application Performance).
    /// HttpClient is injected via DI (registered as a typed client in Program.cs).
    ///
    /// If the API is unreachable, a sensible fallback rate is returned and the
    /// error is logged — ensuring the application never crashes on API failure.
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyService> _logger;

        // Fallback rate used if the API is unavailable
        private const decimal FallbackRate = 18.75m;

        public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger)
        {
            _httpClient = httpClient;
            _logger     = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            try
            {
                // PRESENTATION POINT: 'await' here is LU4 (Async/Await).
                // Without await, the thread would be BLOCKED waiting for the API response.
                // With await, the thread is RELEASED to handle other requests while waiting.
                // This is what makes web apps scalable — they don't waste threads on waiting.
                var response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
                response.EnsureSuccessStatusCode(); // Throws if the HTTP status is 4xx or 5xx

                // Also async — reading the response body doesn't block
                var json = await response.Content.ReadAsStringAsync();

                // Parse the JSON response from the API.
                // The API returns: { "result": "success", "rates": { "ZAR": 18.75, "EUR": 0.91, ... } }
                using var doc = JsonDocument.Parse(json);

                // Navigate the JSON tree to get just the ZAR value
                if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("ZAR", out var zarRate))
                {
                    var rate = zarRate.GetDecimal();
                    _logger.LogInformation("Fetched USD-ZAR rate: {Rate}", rate);
                    return rate;
                }

                _logger.LogWarning("ZAR rate not found in API response. Using fallback: {Rate}", FallbackRate);
                return FallbackRate;
            }
            catch (Exception ex)
            {
                // PRESENTATION POINT: Graceful degradation.
                // If the API is down or the internet is unavailable, we don't crash.
                // We log the error and return a sensible fallback rate (18.75).
                // The app keeps working — the user just sees a slightly outdated rate.
                _logger.LogError(ex, "Currency API unreachable. Using fallback rate: {Rate}", FallbackRate);
                return FallbackRate;
            }
        }
    }
}
