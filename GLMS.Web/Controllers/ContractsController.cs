using GLMS.Shared.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Web.Controllers
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// MVC Controller for the Contracts section of the GLMS frontend.
    ///
    /// WHAT CHANGED IN PART 3 (SOA Refactoring):
    /// Previously: directly called EF Core for all CRUD + used Factory/Observer patterns here.
    /// Now: all data operations delegate to ApiService → GLMS.Api.
    ///
    /// The Factory Method Pattern and Observer Pattern still run — they just run
    /// INSIDE GLMS.Api when the API receives the POST /api/contracts request.
    /// This controller just sends the form data to the API and handles the response.
    ///
    /// PDF UPLOAD:
    /// PDF files are sent as multipart form data to GLMS.Api, which saves them
    /// to its own wwwroot/uploads folder and stores the path in the database.
    /// </summary>
    public class ContractsController : Controller
    {
        private readonly ApiService _api;

        public ContractsController(ApiService api)
        {
            _api = api;
        }

        private bool IsAuthenticated() => HttpContext.Session.GetString("JwtToken") != null;

        // Populate dropdowns for Create/Edit forms
        private async Task PopulateViewBagsAsync(int? selectedClientId = null)
        {
            var clients = await _api.GetClientsAsync();
            ViewBag.Clients       = new SelectList(clients.OrderBy(c => c.Name), "Id", "Name", selectedClientId);
            ViewBag.ServiceLevels = new SelectList(Enum.GetNames(typeof(ServiceLevel)));
            ViewBag.ContractTypes = new SelectList(Enum.GetNames(typeof(ContractType)));
            ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ContractStatus)));
        }

        // =====================================================================
        // INDEX — with Search/Filter
        // =====================================================================

        public async Task<IActionResult> Index(string? statusFilter, DateTime? startFrom, DateTime? startTo)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            var contracts = await _api.GetContractsAsync(
                statusFilter,
                startFrom?.ToString("yyyy-MM-dd"),
                startTo?.ToString("yyyy-MM-dd"));

            ViewBag.StatusFilter  = statusFilter;
            ViewBag.StartFrom     = startFrom?.ToString("yyyy-MM-dd");
            ViewBag.StartTo       = startTo?.ToString("yyyy-MM-dd");
            ViewBag.StatusOptions = new SelectList(Enum.GetNames(typeof(ContractStatus)));

            return View(contracts);
        }

        // =====================================================================
        // DETAILS
        // =====================================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var contract = await _api.GetContractAsync(id.Value);
            if (contract == null) return NotFound();
            return View(contract);
        }

        // =====================================================================
        // CREATE — Factory Method Pattern runs inside GLMS.Api
        // =====================================================================

        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            await PopulateViewBagsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int clientId,
            DateTime startDate,
            DateTime endDate,
            string serviceLevel,
            string contractType,
            IFormFile? signedAgreement)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");

            // PRESENTATION POINT: The Factory Method Pattern still runs here —
            // but it runs inside GLMS.Api when it receives this request.
            // ContractFactoryResolver.GetFactory(contractType) is called on the API side.
            var (success, error) = await _api.CreateContractAsync(
                clientId, startDate, endDate, serviceLevel, contractType, signedAgreement);

            if (success)
            {
                TempData["Success"] = $"Contract created using the {contractType} factory.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", error ?? "Failed to create contract.");
            await PopulateViewBagsAsync(clientId);
            return View();
        }

        // =====================================================================
        // EDIT — Observer Pattern runs inside GLMS.Api on status change
        // =====================================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var contract = await _api.GetContractAsync(id.Value);
            if (contract == null) return NotFound();
            await PopulateViewBagsAsync(contract.ClientId);
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Contract contract, IFormFile? signedAgreement)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id != contract.Id) return NotFound();

            // PRESENTATION POINT: The Observer Pattern still runs —
            // GLMS.Api.ContractsController.Put() detects the status change
            // and calls ContractStatusSubject.NotifyAsync() on the API side.
            var success = await _api.UpdateContractAsync(id, contract, signedAgreement);

            if (success)
            {
                TempData["Success"] = "Contract updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Failed to update contract. Please try again.");
            await PopulateViewBagsAsync(contract.ClientId);
            return View(contract);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var contract = await _api.GetContractAsync(id.Value);
            if (contract == null) return NotFound();
            return View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            await _api.DeleteContractAsync(id);
            TempData["Success"] = "Contract deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DOWNLOAD Signed Agreement — proxies the file from GLMS.Api
        // =====================================================================

        public async Task<IActionResult> DownloadAgreement(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Account");
            // Redirect the browser directly to the API download endpoint
            // The API serves the file from its own wwwroot/uploads folder
            var apiUrl = $"{HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ApiBaseUrl"]}/api/contracts/{id}/download";
            return Redirect(apiUrl);
        }
    }
}
