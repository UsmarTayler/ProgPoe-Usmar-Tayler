using GLMS.Shared.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Web.Controllers
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// MVC Controller for Service Requests in the GLMS frontend.
    ///
    /// WHAT CHANGED IN PART 3 (SOA Refactoring):
    /// Previously: used ApplicationDbContext + Strategy Pattern directly.
    /// Now: calls ApiService → GLMS.Api for all data operations.
    ///
    /// The Strategy Pattern (USD→ZAR conversion) still runs — it runs inside
    /// GLMS.Api when it receives POST /api/servicerequests.
    /// The workflow rule (no ServiceRequest on Expired/OnHold contracts) also
    /// runs inside the API — the API returns a 400 Bad Request if violated,
    /// and this controller shows the error message to the user.
    /// </summary>
    public class ServiceRequestsController : Controller
    {
        private readonly ApiService _api;

        public ServiceRequestsController(ApiService api)
        {
            _api = api;
        }

        private bool IsAuthenticated() => HttpContext.Session.GetString("JwtToken") != null;

        // Populate contract dropdown for Create form (only Active/Draft contracts)
        private async Task PopulateContractDropdownAsync(int? selectedContractId = null)
        {
            var allContracts = await _api.GetContractsAsync();
            var eligible     = allContracts
                .Where(c => c.Status != ContractStatus.Expired && c.Status != ContractStatus.OnHold)
                .OrderBy(c => c.Client?.Name)
                .Select(c => new
                {
                    c.Id,
                    Display = $"{c.Client?.Name ?? "?"} — Contract #{c.Id} ({c.Status})"
                })
                .ToList();

            ViewBag.Contracts     = new SelectList(eligible, "Id", "Display", selectedContractId);
            ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ServiceRequestStatus)));
        }

        // =====================================================================
        // INDEX
        // =====================================================================

        public async Task<IActionResult> Index(int? contractId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            var requests = await _api.GetServiceRequestsAsync(contractId);
            ViewBag.ContractId = contractId;
            return View(requests);
        }

        // =====================================================================
        // DETAILS
        // =====================================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var request = await _api.GetServiceRequestAsync(id.Value);
            if (request == null) return NotFound();
            return View(request);
        }

        // =====================================================================
        // CREATE — Strategy Pattern + workflow rule runs inside GLMS.Api
        // =====================================================================

        public async Task<IActionResult> Create(int? contractId)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            // Get live exchange rate from API (which fetches from ExchangeRate-API)
            ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
            await PopulateContractDropdownAsync(contractId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            // Remove navigation-property validation errors (they're set by the API)
            ModelState.Remove("Contract");

            if (!ModelState.IsValid)
            {
                ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
                await PopulateContractDropdownAsync(serviceRequest.ContractId);
                return View(serviceRequest);
            }

            // PRESENTATION POINT: Strategy Pattern + workflow rule run inside GLMS.Api.
            // The API validates the contract status, fetches the live rate,
            // applies UsdToZarStrategy, and saves CostZAR to the database.
            var (success, error) = await _api.CreateServiceRequestAsync(serviceRequest);

            if (success)
            {
                TempData["Success"] = "Service Request created successfully. ZAR cost calculated by the API.";
                return RedirectToAction(nameof(Index));
            }

            // API returned an error (e.g., contract is Expired/OnHold)
            ModelState.AddModelError("ContractId", error ?? "Failed to create service request.");
            ViewBag.ExchangeRate = await _api.GetExchangeRateAsync();
            await PopulateContractDropdownAsync(serviceRequest.ContractId);
            return View(serviceRequest);
        }

        // =====================================================================
        // EDIT
        // =====================================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var request = await _api.GetServiceRequestAsync(id.Value);
            if (request == null) return NotFound();
            ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ServiceRequestStatus)));
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceRequest serviceRequest)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id != serviceRequest.Id) return NotFound();

            ModelState.Remove("Contract");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ServiceRequestStatus)));
                return View(serviceRequest);
            }

            var success = await _api.UpdateServiceRequestAsync(id, serviceRequest);
            if (success)
            {
                TempData["Success"] = "Service Request updated.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Failed to update. Please try again.");
            ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ServiceRequestStatus)));
            return View(serviceRequest);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var request = await _api.GetServiceRequestAsync(id.Value);
            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            await _api.DeleteServiceRequestAsync(id);
            TempData["Success"] = "Service Request deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // AJAX: get live rate (proxied from the API)
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> GetRate()
        {
            if (!IsAuthenticated()) return Unauthorized();
            var rate = await _api.GetExchangeRateAsync();
            return Json(new { rate, zarFormatted = rate.ToString("F4") });
        }
    }
}
