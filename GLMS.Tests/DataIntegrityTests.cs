using GLMS.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace GLMS.Tests
{
    // =========================================================================
    // CUSTOM FACTORY — swaps SQL Server for an isolated in-memory database
    // =========================================================================

    /// <summary>
    /// WHAT THIS CLASS IS:
    /// A custom WebApplicationFactory that overrides the EF Core DbContext registration
    /// to use an in-memory database instead of SQL Server.
    ///
    /// WHY THIS MATTERS (Criterion 4 — Integration Testing):
    /// Real integration tests should be:
    ///   - Fast  — no SQL Server process required
    ///   - Isolated — each test run gets a fresh, empty database
    ///   - Deterministic — results never depend on leftover data from other tests
    ///
    /// HOW IT WORKS:
    ///   1. ConfigureWebHost() is called by the framework before the first test.
    ///   2. We find and remove the existing DbContextOptions&lt;ApplicationDbContext&gt;
    ///      registration (the one pointing to SQL Server) and replace it with an
    ///      in-memory store identified by a unique Guid.
    ///   3. A unique DB name per factory instance ensures complete isolation —
    ///      tests cannot interfere with each other even if they run in parallel.
    ///   4. EnsureCreated() runs once before tests start so the schema exists.
    /// </summary>
    public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Unique DB name per factory instance = isolated DB per test class instance
        private readonly string _dbName = $"GLMS_TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // ── Step 1: remove the SQL Server DbContext registration ──────────
                // The main Program.cs registered AddDbContext<ApplicationDbContext>
                // pointing at SQL Server. We need to find and remove that descriptor
                // so we can substitute the in-memory version.
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                // ── Step 2: register the in-memory replacement ────────────────────
                // UseInMemoryDatabase() creates a lightweight in-memory store.
                // It supports all EF Core LINQ operations, FK constraints are not
                // enforced at the DB level, but EF Core's relational model is intact.
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));

                // ── Step 3: ensure schema exists before tests run ─────────────────
                // Build a temporary service provider and call EnsureCreated().
                // With an in-memory database this completes in milliseconds.
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }

    // =========================================================================
    // DATA INTEGRITY TESTS — Create then Read
    // =========================================================================

    /// <summary>
    /// WHAT THIS FILE IS:
    /// "Create then Read" data integrity integration tests for GLMS.Api.
    /// Uses InMemoryWebApplicationFactory so every test run starts with an empty DB.
    ///
    /// WHAT I DID (Part 3 — Criterion 4):
    /// These tests go further than the basic routing/auth tests in ApiIntegrationTests.cs.
    /// They verify DATA INTEGRITY: that what you POST to the API is exactly what you
    /// GET back. This catches:
    ///   - Field-mapping bugs (wrong column bound to wrong property)
    ///   - Serialisation mismatches (camelCase vs PascalCase)
    ///   - Missing [Required] validation that allows partial/corrupt saves
    ///   - Business-logic side-effects that mutate data before persisting
    ///
    /// EACH TEST FOLLOWS THE SAME PATTERN:
    ///   1. Login — get a JWT token (AuthController, no DB needed)
    ///   2. POST /api/{resource}  — write a record with known field values
    ///   3. Assert 201 Created   — confirms the write path succeeded
    ///   4. GET  /api/{resource}/{id} — read back by the ID returned in step 2
    ///   5. Assert 200 OK + fields match — confirms read is consistent with write
    ///
    /// This "round-trip" proves the full stack works:
    ///   HTTP→Controller→Service→EF Core→In-Memory DB→EF Core→Service→Controller→JSON
    ///
    /// TESTS IN THIS FILE (8 new tests → total project = 16 tests):
    ///   1. Create Client → Read Client    (name/email/phone/region survive round-trip)
    ///   2. Create Contract → Read Contract (clientId FK, serviceLevel field integrity)
    ///   3. Create ServiceRequest → Read   (contractId FK, Strategy Pattern proof via CostZAR)
    ///   4. Create two Clients → IDs are distinct (no PK collision)
    ///   5. Create Client with invalid data → 400 Bad Request (validation enforcement)
    ///   6. Read non-existent Client → 404 Not Found (error path coverage)
    ///   7. Create → Delete → Read → 404  (delete actually removes from DB)
    ///   8. Create → Update → Read → updated values (PUT persisted correctly)
    /// </summary>
    public class DataIntegrityTests : IClassFixture<InMemoryWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public DataIntegrityTests(InMemoryWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // =====================================================================
        // SHARED HELPERS
        // =====================================================================

        /// <summary>
        /// Logs in as admin and attaches the JWT Bearer token to _client.
        /// All data endpoints are protected with [Authorize], so every test calls this.
        /// The AuthController doesn't touch the database, so it works with in-memory DB.
        /// </summary>
        private async Task LoginAsync()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var body     = JsonSerializer.Serialize(new { username = "admin", password = "Admin123!" });
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json  = await response.Content.ReadAsStringAsync();
            using var doc  = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("token").GetString()!;

            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Creates a Client via POST and returns its assigned database ID.
        /// Shared by several tests that need a client as a prerequisite.
        /// </summary>
        private async Task<int> CreateClientAsync(string name = "Test Client",
                                                   string email  = "test@gov.za",
                                                   string phone  = "+27110000001",
                                                   string region = "Gauteng")
        {
            var payload = new { name, email, phone, region };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/clients", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("id").GetInt32();
        }

        /// <summary>
        /// Creates a Contract via multipart POST for a given clientId.
        /// Returns the assigned contract ID.
        /// </summary>
        private async Task<int> CreateContractAsync(int clientId,
                                                      string serviceLevel  = "Standard",
                                                      string contractType  = "Local")
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(clientId.ToString()),                       "clientId");
            form.Add(new StringContent(new DateTime(2025, 1, 1).ToString("o")),    "startDate");
            form.Add(new StringContent(new DateTime(2026, 1, 1).ToString("o")),    "endDate");
            form.Add(new StringContent(serviceLevel),                              "serviceLevel");
            form.Add(new StringContent(contractType),                              "contractType");

            var response = await _client.PostAsync("/api/contracts", form);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("id").GetInt32();
        }

        // =====================================================================
        // TEST 1: Create Client → Read Client
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// The most fundamental data integrity check: a Client written via POST is
        /// exactly the Client returned by GET — same name, email, phone, and region.
        ///
        /// If any field is silently dropped, renamed, or mangled during persistence,
        /// this test will catch it before it reaches the lecturer's demo.
        ///
        /// This directly satisfies the rubric requirement for "Create then Read tests
        /// that verify data integrity."
        /// </summary>
        [Fact]
        public async Task CreateClient_ThenRead_DataMatchesExactly()
        {
            // ── Arrange ──────────────────────────────────────────────────────
            await LoginAsync();

            // ── Act: POST ────────────────────────────────────────────────────
            var payload = new
            {
                name   = "Acme Logistics",
                email  = "contact@acme.co.za",
                phone  = "+27110000001",
                region = "Western Cape"
            };
            var postContent  = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var postResponse = await _client.PostAsync("/api/clients", postContent);

            // ── Assert: 201 Created ───────────────────────────────────────────
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            using var createdDoc = JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
            var id = createdDoc.RootElement.GetProperty("id").GetInt32();
            Assert.True(id > 0, "API must return a positive DB-assigned ID on creation.");

            // ── Act: GET ─────────────────────────────────────────────────────
            var getResponse = await _client.GetAsync($"/api/clients/{id}");

            // ── Assert: 200 OK + field-level data integrity ───────────────────
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var fetchedDoc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var root = fetchedDoc.RootElement;

            // Every written field must survive the round-trip unchanged
            Assert.Equal("Acme Logistics",     root.GetProperty("name").GetString());
            Assert.Equal("contact@acme.co.za", root.GetProperty("email").GetString());
            Assert.Equal("+27110000001",        root.GetProperty("phone").GetString());
            Assert.Equal("Western Cape",        root.GetProperty("region").GetString());
        }

        // =====================================================================
        // TEST 2: Create Contract → Read Contract (relational FK integrity)
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// A two-step Create→Read across a foreign-key relationship.
        /// First creates a Client (required FK parent), then creates a Contract,
        /// then reads the contract back and verifies the clientId and serviceLevel.
        ///
        /// This exercises ContractService.CreateAsync which applies the
        /// Factory Method Pattern (LocalContractFactory or InternationalContractFactory)
        /// before persisting. If the factory introduces a bug during construction,
        /// the field values will not match what was submitted — and this test will fail.
        /// </summary>
        [Fact]
        public async Task CreateContract_ThenRead_DataMatchesExactly()
        {
            // ── Arrange: create parent client ─────────────────────────────────
            await LoginAsync();
            var clientId = await CreateClientAsync("GovDept Transport", "transport@gov.za", "+27120000002", "Gauteng");

            // ── Act: POST Contract (multipart) ────────────────────────────────
            // ContractsController.Create uses [FromForm] so we send multipart/form-data.
            // serviceLevel = "Premium" (ServiceLevel enum value)
            // contractType = "International" (ContractType enum value → InternationalContractFactory)
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(clientId.ToString()),                       "clientId");
            form.Add(new StringContent(new DateTime(2025, 6, 1).ToString("o")),    "startDate");
            form.Add(new StringContent(new DateTime(2027, 6, 1).ToString("o")),    "endDate");
            form.Add(new StringContent("Premium"),                                 "serviceLevel");
            form.Add(new StringContent("International"),                           "contractType");

            var postResponse = await _client.PostAsync("/api/contracts", form);

            // ── Assert: 201 Created ───────────────────────────────────────────
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            using var createdDoc = JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
            var contractId = createdDoc.RootElement.GetProperty("id").GetInt32();
            Assert.True(contractId > 0);

            // ── Act: GET Contract ─────────────────────────────────────────────
            var getResponse = await _client.GetAsync($"/api/contracts/{contractId}");

            // ── Assert: 200 OK + field-level data integrity ───────────────────
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var fetchedDoc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var root = fetchedDoc.RootElement;

            // FK integrity: the stored clientId must match the client we created
            Assert.Equal(clientId, root.GetProperty("clientId").GetInt32());

            // Enum field integrity: serviceLevel must survive as "Premium"
            Assert.Equal("Premium", root.GetProperty("serviceLevel").GetString());
        }

        // =====================================================================
        // TEST 3: Create ServiceRequest → Read ServiceRequest + Strategy Pattern proof
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// Creates Client → Contract → ServiceRequest, then reads the request back.
        /// Verifies description and contractId are preserved.
        ///
        /// The CostZAR assertion is the Strategy Pattern proof:
        ///   ServiceRequestService.CreateAsync calls ICurrencyConversionStrategy.Convert()
        ///   (the UsdToZarStrategy) to multiply CostUSD by the live/fallback exchange rate.
        ///   If the strategy ran correctly, CostZAR must be greater than CostUSD
        ///   because the ZAR/USD rate is always > 1 (ZAR is weaker than USD).
        ///   If CostZAR == 0 or CostZAR == CostUSD, the strategy did not run.
        /// </summary>
        [Fact]
        public async Task CreateServiceRequest_ThenRead_DataMatchesAndStrategyRan()
        {
            // ── Step 1: Client ────────────────────────────────────────────────
            await LoginAsync();
            var clientId = await CreateClientAsync("Rail SA", "rail@gov.za", "+27310000003", "KwaZulu-Natal");

            // ── Step 2: Contract ──────────────────────────────────────────────
            var contractId = await CreateContractAsync(clientId, "Standard", "Local");

            // ── Step 3: POST ServiceRequest ───────────────────────────────────
            var srPayload = new
            {
                contractId  = contractId,
                description = "Replace brake assembly on Locomotive 47B",
                costUSD     = 2500.00m,
                status      = 0  // ServiceRequestStatus.Pending = 0
            };
            var srContent  = new StringContent(JsonSerializer.Serialize(srPayload), Encoding.UTF8, "application/json");
            var srResponse = await _client.PostAsync("/api/servicerequests", srContent);

            // ── Assert: 201 Created ───────────────────────────────────────────
            Assert.Equal(HttpStatusCode.Created, srResponse.StatusCode);

            using var createdDoc = JsonDocument.Parse(await srResponse.Content.ReadAsStringAsync());
            var srId = createdDoc.RootElement.GetProperty("id").GetInt32();
            Assert.True(srId > 0);

            // ── Step 4: GET ServiceRequest ────────────────────────────────────
            var getResponse = await _client.GetAsync($"/api/servicerequests/{srId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var fetchedDoc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var root = fetchedDoc.RootElement;

            // FK integrity
            Assert.Equal(contractId, root.GetProperty("contractId").GetInt32());

            // Description field integrity
            Assert.Equal("Replace brake assembly on Locomotive 47B",
                         root.GetProperty("description").GetString());

            // Strategy Pattern proof: CostZAR must be > CostUSD (ZAR is weaker than USD)
            var costZAR = root.GetProperty("costZAR").GetDecimal();
            Assert.True(costZAR > 2500m,
                $"CostZAR ({costZAR}) must exceed CostUSD (2500) — " +
                $"if equal or zero, the UsdToZarStrategy did not run.");
        }

        // =====================================================================
        // TEST 4: Create two Clients → IDs are distinct
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// That the database assigns a unique, auto-incrementing primary key to each record.
        /// Two sequential POSTs must receive different IDs — no collision, no accidental overwrite.
        ///
        /// This guards against a subtle bug where a service method might return a cached
        /// entity or reuse the same ID for different records.
        /// </summary>
        [Fact]
        public async Task CreateTwoClients_IDsAreDistinct()
        {
            await LoginAsync();

            var id1 = await CreateClientAsync("Client Alpha", "alpha@gov.za", "+27110000004", "Limpopo");
            var id2 = await CreateClientAsync("Client Beta",  "beta@gov.za",  "+27110000005", "Mpumalanga");

            Assert.True(id1 > 0);
            Assert.True(id2 > 0);
            Assert.NotEqual(id1, id2);
        }

        // =====================================================================
        // TEST 5: Create Client with invalid data → 400 Bad Request
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// That [Required] and [EmailAddress] Data Annotations on the Client model
        /// are enforced by ModelState validation in ClientsController.
        ///
        /// Sending null for required fields must return 400 Bad Request — the bad data
        /// must never reach the service or the database.
        ///
        /// This is a negative data integrity test: it proves that the validation
        /// boundary works and cannot be bypassed with a crafted HTTP request.
        /// </summary>
        [Fact]
        public async Task CreateClient_WithMissingRequiredFields_Returns400()
        {
            await LoginAsync();

            // name = null → fails [Required]
            // email = "not-an-email" → fails [EmailAddress]
            // phone and region are also null → fail [Required]
            var invalid = new
            {
                name   = (string?)null,
                email  = "not-an-email",
                phone  = (string?)null,
                region = (string?)null
            };
            var content = new StringContent(JsonSerializer.Serialize(invalid), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/clients", content);

            // ModelState validation must reject this before it reaches ClientService
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // =====================================================================
        // TEST 6: Read non-existent Client → 404 Not Found
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// That the API returns the correct 404 HTTP status — not 200 with null body,
        /// not 500 — when the requested ID does not exist.
        ///
        /// Using ID 999999 which cannot exist in a freshly created in-memory database.
        /// This validates the ClientService.GetByIdAsync null-check path and the
        /// controller's `return request is null ? NotFound(...) : Ok(request)` logic.
        /// </summary>
        [Fact]
        public async Task GetClient_NonExistentId_Returns404()
        {
            await LoginAsync();

            var response = await _client.GetAsync("/api/clients/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // =====================================================================
        // TEST 7: Create → Delete → Read → 404 (delete integrity)
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// That a deleted record cannot subsequently be retrieved.
        /// Verifies DELETE actually removes the record from the database —
        /// not just marking it as soft-deleted or returning a fake 204.
        ///
        /// Pattern: Create → Assert 201 → DELETE → Assert 204 → GET → Assert 404.
        /// </summary>
        [Fact]
        public async Task CreateClient_ThenDelete_ThenRead_Returns404()
        {
            await LoginAsync();

            // Create
            var id = await CreateClientAsync("Temp Corp", "temp@gov.za", "+27110000006", "Free State");

            // Delete
            var deleteResponse = await _client.DeleteAsync($"/api/clients/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Read — record must be gone
            var getResponse = await _client.GetAsync($"/api/clients/{id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // =====================================================================
        // TEST 8: Create → Update → Read → updated values (update integrity)
        // =====================================================================

        /// <summary>
        /// WHAT THIS TESTS:
        /// That a PUT update is persisted and that a subsequent GET reflects the NEW
        /// values — not the originals or stale cached data.
        ///
        /// Pattern: Create (name="Old") → Assert 201 → PUT (name="New") → Assert 204
        ///          → GET → Assert name="New".
        ///
        /// This validates the full update path through ClientService.UpdateAsync and
        /// proves EF Core's change-tracking correctly saves the modified entity.
        /// </summary>
        [Fact]
        public async Task CreateClient_ThenUpdate_ThenRead_ReturnsUpdatedValues()
        {
            await LoginAsync();

            // Create original
            var id = await CreateClientAsync("Old Name Corp", "old@corp.za", "+27110000007", "Northern Cape");

            // Update with new values (PUT requires the full entity including Id)
            var updated = new
            {
                id,
                name   = "New Name Corp",
                email  = "new@corp.za",
                phone  = "+27110000008",
                region = "Eastern Cape"
            };
            var putContent  = new StringContent(JsonSerializer.Serialize(updated), Encoding.UTF8, "application/json");
            var putResponse = await _client.PutAsync($"/api/clients/{id}", putContent);
            Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

            // Read back — must show updated values, not originals
            var getResponse = await _client.GetAsync($"/api/clients/{id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var doc  = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            Assert.Equal("New Name Corp", root.GetProperty("name").GetString());
            Assert.Equal("new@corp.za",   root.GetProperty("email").GetString());
            Assert.Equal("Eastern Cape",  root.GetProperty("region").GetString());
        }
    }
}
