using GLMS.Api.Services;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers
{
    /// <summary>
    /// REST API endpoints for ServiceRequests.
    ///
    /// PART 3 — THIN CONTROLLER:
    /// Strategy Pattern + workflow rules live in ServiceRequestService.
    /// This controller only handles HTTP concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly IServiceRequestService _service;

        public ServiceRequestsController(IServiceRequestService service)
        {
            _service = service;
        }

        /// <summary>GET /api/servicerequests — list all, with optional ?contractId= filter.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int? contractId)
        {
            var requests = await _service.GetAllAsync(contractId);
            return Ok(requests);
        }

        /// <summary>GET /api/servicerequests/{id} — single service request or 404.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _service.GetByIdAsync(id);
            return request is null ? NotFound(new { message = $"ServiceRequest {id} not found." }) : Ok(request);
        }

        /// <summary>
        /// GET /api/servicerequests/rate — returns the live USD→ZAR rate.
        /// Route must be declared BEFORE {id:int} to avoid routing conflict.
        /// </summary>
        [HttpGet("rate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRate()
        {
            var rate = await _service.GetExchangeRateAsync();
            return Ok(new { rate, zarFormatted = rate.ToString("F4") });
        }

        /// <summary>
        /// POST /api/servicerequests — creates a service request.
        /// Strategy Pattern (USD→ZAR) and workflow rule run in ServiceRequestService.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ServiceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (created, error) = await _service.CreateAsync(request);
            if (created is null) return BadRequest(new { message = error });

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>PUT /api/servicerequests/{id} — updates status/description.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ServiceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _service.UpdateAsync(id, request);
            return success ? NoContent() : NotFound(new { message = $"ServiceRequest {id} not found." });
        }

        /// <summary>DELETE /api/servicerequests/{id} — deletes a service request.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound(new { message = $"ServiceRequest {id} not found." });
        }
    }
}
