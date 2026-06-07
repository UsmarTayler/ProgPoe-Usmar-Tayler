using GLMS.Shared.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GLMS.Web.Services
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The central HTTP client service for the GLMS MVC frontend.
    /// Every controller that needs data calls a method here instead of
    /// querying the database directly.
    ///
    /// WHAT I DID (Part 3):
    /// I replaced all EF Core database calls in the MVC controllers with calls
    /// to this service. ApiService wraps HttpClient and handles:
    ///   1. Reading the JWT token from the session
    ///   2. Attaching the token to every outgoing request ("Bearer {token}")
    ///   3. Deserialising the JSON response into C# model objects
    ///   4. Returning null / empty lists if the API responds with 404 or errors
    ///
    /// WHY SOA:
    /// The MVC app is now the "Presentation Layer" — it only deals with
    /// what the user sees. The "Service Layer" (business logic + DB) lives
    /// in GLMS.Api. They communicate via HTTP and JSON — this is SOA.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Shared JSON options — must match the serialiser configuration in GLMS.Api/Program.cs.
        // JsonStringEnumConverter: the API sends enums as strings ("Premium", not 2).
        // PropertyNameCaseInsensitive: API uses camelCase JSON, C# models use PascalCase.
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient          = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        // =====================================================================
        // PRIVATE HELPER: Attach JWT token to every request
        // Reads the token stored in the session after login.
        // =====================================================================
        private void AttachToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            _httpClient.DefaultRequestHeaders.Authorization = !string.IsNullOrEmpty(token)
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
        }

        // =====================================================================
        // AUTH
        // =====================================================================

        /// <summary>
        /// Calls POST /api/auth/login and returns the JWT token string, or null on failure.
        /// </summary>
        public async Task<string?> LoginAsync(string username, string password)
        {
            var body    = JsonSerializer.Serialize(new { username, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }

        // =====================================================================
        // DASHBOARD
        // =====================================================================

        /// <summary>Returns dashboard stats from GET /api/dashboard.</summary>
        public async Task<DashboardStats?> GetDashboardStatsAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("/api/dashboard");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DashboardStats>(json, _jsonOptions);
        }

        // =====================================================================
        // CLIENTS
        // =====================================================================

        public async Task<List<Client>> GetClientsAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("/api/clients");
            if (!response.IsSuccessStatusCode) return new List<Client>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Client>>(json, _jsonOptions) ?? new List<Client>();
        }

        public async Task<Client?> GetClientAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"/api/clients/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Client>(json, _jsonOptions);
        }

        public async Task<bool> CreateClientAsync(Client client)
        {
            AttachToken();
            var body     = JsonSerializer.Serialize(client);
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/clients", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateClientAsync(int id, Client client)
        {
            AttachToken();
            var body     = JsonSerializer.Serialize(client);
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/clients/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.DeleteAsync($"/api/clients/{id}");
            return response.IsSuccessStatusCode;
        }

        // =====================================================================
        // CONTRACTS
        // =====================================================================

        public async Task<List<Contract>> GetContractsAsync(string? statusFilter = null, string? startFrom = null, string? startTo = null)
        {
            AttachToken();
            var query = "";
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(statusFilter)) parts.Add($"status={statusFilter}");
            if (!string.IsNullOrEmpty(startFrom))    parts.Add($"startFrom={startFrom}");
            if (!string.IsNullOrEmpty(startTo))      parts.Add($"startTo={startTo}");
            if (parts.Any()) query = "?" + string.Join("&", parts);

            var response = await _httpClient.GetAsync($"/api/contracts{query}");
            if (!response.IsSuccessStatusCode) return new List<Contract>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Contract>>(json, _jsonOptions) ?? new List<Contract>();
        }

        public async Task<Contract?> GetContractAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"/api/contracts/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Contract>(json, _jsonOptions);
        }

        /// <summary>
        /// Creates a contract via multipart form (supports PDF file upload).
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> CreateContractAsync(
            int clientId, DateTime startDate, DateTime endDate,
            string serviceLevel, string contractType, IFormFile? signedAgreement)
        {
            AttachToken();
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(clientId.ToString()),              "clientId");
            form.Add(new StringContent(startDate.ToString("yyyy-MM-dd")), "startDate");
            form.Add(new StringContent(endDate.ToString("yyyy-MM-dd")),   "endDate");
            form.Add(new StringContent(serviceLevel),                     "serviceLevel");
            form.Add(new StringContent(contractType),                     "contractType");

            if (signedAgreement != null)
            {
                var stream  = signedAgreement.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "signedAgreement", signedAgreement.FileName);
            }

            var response = await _httpClient.PostAsync("/api/contracts", form);
            if (response.IsSuccessStatusCode) return (true, null);

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<bool> UpdateContractAsync(int id, Contract contract, IFormFile? signedAgreement)
        {
            AttachToken();
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(contract.Id.ToString()),                        "id");
            form.Add(new StringContent(contract.ClientId.ToString()),                  "clientId");
            form.Add(new StringContent(contract.StartDate.ToString("yyyy-MM-dd")),     "startDate");
            form.Add(new StringContent(contract.EndDate.ToString("yyyy-MM-dd")),       "endDate");
            form.Add(new StringContent(contract.Status.ToString()),                    "status");
            form.Add(new StringContent(contract.ServiceLevel.ToString()),              "serviceLevel");
            form.Add(new StringContent(contract.ContractType.ToString()),              "contractType");

            if (signedAgreement != null)
            {
                var stream  = signedAgreement.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "signedAgreement", signedAgreement.FileName);
            }

            var response = await _httpClient.PutAsync($"/api/contracts/{id}", form);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PatchContractStatusAsync(int id, string status)
        {
            AttachToken();
            var body    = JsonSerializer.Serialize(new { status });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"/api/contracts/{id}/status", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteContractAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.DeleteAsync($"/api/contracts/{id}");
            return response.IsSuccessStatusCode;
        }

        // =====================================================================
        // SERVICE REQUESTS
        // =====================================================================

        public async Task<List<ServiceRequest>> GetServiceRequestsAsync(int? contractId = null)
        {
            AttachToken();
            var url      = contractId.HasValue ? $"/api/servicerequests?contractId={contractId}" : "/api/servicerequests";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<ServiceRequest>();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ServiceRequest>>(json, _jsonOptions) ?? new List<ServiceRequest>();
        }

        public async Task<ServiceRequest?> GetServiceRequestAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.GetAsync($"/api/servicerequests/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ServiceRequest>(json, _jsonOptions);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateServiceRequestAsync(ServiceRequest request)
        {
            AttachToken();
            var body     = JsonSerializer.Serialize(request);
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/servicerequests", content);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<bool> UpdateServiceRequestAsync(int id, ServiceRequest request)
        {
            AttachToken();
            var body     = JsonSerializer.Serialize(request);
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/servicerequests/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteServiceRequestAsync(int id)
        {
            AttachToken();
            var response = await _httpClient.DeleteAsync($"/api/servicerequests/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<decimal> GetExchangeRateAsync()
        {
            AttachToken();
            var response = await _httpClient.GetAsync("/api/servicerequests/rate");
            if (!response.IsSuccessStatusCode) return 18.75m;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("rate", out var rateProp)
                ? rateProp.GetDecimal()
                : 18.75m;
        }
    }

    /// <summary>DTO for the dashboard stats endpoint response.</summary>
    public class DashboardStats
    {
        public int     TotalClients         { get; set; }
        public int     TotalContracts       { get; set; }
        public int     ActiveContracts      { get; set; }
        public int     TotalServiceRequests { get; set; }
        public int     PendingRequests      { get; set; }
        public decimal TotalRevenue         { get; set; }
    }
}
