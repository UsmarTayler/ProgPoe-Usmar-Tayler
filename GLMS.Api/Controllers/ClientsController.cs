using GLMS.Api.Services;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers
{
    /// <summary>
    /// REST API endpoints for Clients.
    ///
    /// PART 3 — THIN CONTROLLER (Architectural Integrity):
    /// This controller contains ZERO business logic and ZERO database code.
    /// Every action does exactly three things:
    ///   1. Receive the HTTP request and extract parameters
    ///   2. Call the service method
    ///   3. Map the result to the correct HTTP response
    ///
    /// All CRUD logic lives in ClientService / IClientService.
    /// This follows the Single Responsibility Principle — the controller's only
    /// responsibility is HTTP (routing, status codes, request/response mapping).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        /// <summary>
        /// PRESENTATION POINT: Dependency Injection.
        /// The controller depends on IClientService (an interface), not ClientService
        /// (the concrete class). This makes it easy to swap implementations for testing.
        /// </summary>
        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        /// <summary>GET /api/clients — returns all clients.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _clientService.GetAllAsync();
            return Ok(clients);
        }

        /// <summary>GET /api/clients/{id} — returns a single client or 404.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            return client is null ? NotFound(new { message = $"Client {id} not found." }) : Ok(client);
        }

        /// <summary>POST /api/clients — creates a new client. Returns 201 Created.</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Client client)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _clientService.CreateAsync(client);
            // 201 Created with Location header pointing to GET /api/clients/{id}
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>PUT /api/clients/{id} — updates an existing client. Returns 204 No Content.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Client client)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await _clientService.UpdateAsync(id, client);
            return success ? NoContent() : NotFound(new { message = $"Client {id} not found." });
        }

        /// <summary>DELETE /api/clients/{id} — deletes a client. Returns 204 No Content.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _clientService.DeleteAsync(id);

            if (!success && error == null) return NotFound(new { message = $"Client {id} not found." });
            if (!success) return Conflict(new { message = error }); // 409: has contracts
            return NoContent();
        }
    }
}
