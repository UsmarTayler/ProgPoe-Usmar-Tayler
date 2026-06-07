using GLMS.Shared.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// MVC Controller for the Clients section of the GLMS frontend.
    ///
    /// WHAT CHANGED IN PART 3 (SOA Refactoring):
    /// Previously: each action injected ApplicationDbContext and ran EF Core queries.
    /// Now: each action calls ApiService which makes an HTTP request to GLMS.Api.
    ///
    ///   Old (Part 2): var clients = await _context.Clients.ToListAsync();
    ///   New (Part 3): var clients = await _api.GetClientsAsync();
    ///                 → ApiService sends GET /api/clients with Bearer token
    ///                 → GLMS.Api queries the DB and returns JSON
    ///                 → ApiService deserialises JSON into List&lt;Client&gt;
    ///
    /// AUTHENTICATION CHECK:
    /// Every action starts by checking if the session has a JWT token.
    /// If not, the user is redirected to the login page.
    /// </summary>
    public class ClientsController : Controller
    {
        private readonly ApiService _api;

        public ClientsController(ApiService api)
        {
            _api = api;
        }

        // Guard helper — redirect to login if not authenticated
        private bool IsAuthenticated() => HttpContext.Session.GetString("JwtToken") != null;

        // =====================================================================
        // INDEX
        // =====================================================================

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            var clients = await _api.GetClientsAsync();
            return View(clients);
        }

        // =====================================================================
        // DETAILS
        // =====================================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var client = await _api.GetClientAsync(id.Value);
            if (client == null) return NotFound();
            return View(client);
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        public IActionResult Create()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(client);

            var success = await _api.CreateClientAsync(client);
            if (success)
            {
                TempData["Success"] = $"Client '{client.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Failed to create client. Please try again.");
            return View(client);
        }

        // =====================================================================
        // EDIT
        // =====================================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var client = await _api.GetClientAsync(id.Value);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id != client.Id) return NotFound();
            if (!ModelState.IsValid) return View(client);

            var success = await _api.UpdateClientAsync(id, client);
            if (success)
            {
                TempData["Success"] = "Client updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Failed to update client. Please try again.");
            return View(client);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var client = await _api.GetClientAsync(id.Value);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            var success = await _api.DeleteClientAsync(id);
            TempData["Success"] = success
                ? "Client deleted successfully."
                : "Could not delete client — they may have associated contracts.";
            return RedirectToAction(nameof(Index));
        }
    }
}
