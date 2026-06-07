using GLMS.Api.Services;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers
{
    /// <summary>
    /// REST API endpoints for Contracts.
    ///
    /// PART 3 — THIN CONTROLLER:
    /// This controller injects IContractService and delegates ALL logic to it.
    /// The Factory Method Pattern, Observer Pattern, file handling, and LINQ queries
    /// live in ContractService — not here.
    ///
    /// Endpoints:
    ///   GET    /api/contracts               — list with optional filters
    ///   GET    /api/contracts/{id}          — single contract
    ///   POST   /api/contracts               — create (multipart, supports PDF)
    ///   PUT    /api/contracts/{id}          — full update
    ///   PATCH  /api/contracts/{id}/status   — update status only (fires Observer)
    ///   DELETE /api/contracts/{id}          — delete
    ///   GET    /api/contracts/{id}/download — download signed agreement PDF
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService    _contractService;
        private readonly IWebHostEnvironment _env;

        public ContractsController(IContractService contractService, IWebHostEnvironment env)
        {
            _contractService = contractService;
            _env             = env;
        }

        private string UploadsPath => Path.Combine(_env.WebRootPath ?? Path.GetTempPath(), "uploads");

        /// <summary>GET /api/contracts — list all contracts, with optional filters.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] string? startFrom,
            [FromQuery] string? startTo)
        {
            var contracts = await _contractService.GetAllAsync(status, startFrom, startTo);
            return Ok(contracts);
        }

        /// <summary>GET /api/contracts/{id} — single contract with client and service requests.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            return contract is null ? NotFound(new { message = $"Contract {id} not found." }) : Ok(contract);
        }

        /// <summary>
        /// POST /api/contracts — creates a contract using the Factory Method Pattern.
        /// Accepts multipart/form-data to support PDF file upload.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(
            [FromForm] int       clientId,
            [FromForm] DateTime  startDate,
            [FromForm] DateTime  endDate,
            [FromForm] string    serviceLevel,
            [FromForm] string    contractType,
            IFormFile?           signedAgreement)
        {
            Directory.CreateDirectory(UploadsPath);
            var (contract, error) = await _contractService.CreateAsync(
                clientId, startDate, endDate, serviceLevel, contractType, signedAgreement, UploadsPath);

            if (contract is null) return BadRequest(new { message = error });
            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
        }

        /// <summary>PUT /api/contracts/{id} — full update of a contract.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            int id,
            [FromForm] Contract contract,
            IFormFile? signedAgreement)
        {
            Directory.CreateDirectory(UploadsPath);
            var (success, error) = await _contractService.UpdateAsync(id, contract, signedAgreement, UploadsPath);

            if (!success && error == "Contract not found.") return NotFound(new { message = error });
            if (!success) return BadRequest(new { message = error });
            return NoContent();
        }

        /// <summary>
        /// PATCH /api/contracts/{id}/status — updates only the Status field.
        /// Fires the Observer Pattern if the status changed.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            var (success, error) = await _contractService.PatchStatusAsync(id, request.Status);

            if (!success && error?.Contains("not found") == true) return NotFound(new { message = error });
            if (!success) return BadRequest(new { message = error });
            return Ok(new { message = $"Contract {id} status updated to {request.Status}." });
        }

        /// <summary>DELETE /api/contracts/{id} — deletes a contract.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _contractService.DeleteAsync(id);
            return success ? NoContent() : NotFound(new { message = $"Contract {id} not found." });
        }

        /// <summary>GET /api/contracts/{id}/download — streams the signed agreement PDF.</summary>
        [HttpGet("{id:int}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(int id)
        {
            var (filePath, fileName) = await _contractService.GetAgreementFileAsync(id);
            if (string.IsNullOrEmpty(filePath)) return NotFound(new { message = "No agreement file found." });

            var fullPath = Path.Combine(_env.WebRootPath ?? Path.GetTempPath(), filePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!System.IO.File.Exists(fullPath)) return NotFound(new { message = "File not found on server." });

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, "application/pdf", fileName ?? "agreement.pdf");
        }
    }

    /// <summary>DTO for the PATCH /api/contracts/{id}/status endpoint.</summary>
    public record StatusUpdateRequest(string Status);
}
