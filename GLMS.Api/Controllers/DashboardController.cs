using GLMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers
{
    /// <summary>
    /// REST API endpoint for dashboard statistics.
    ///
    /// PART 3 — THINNEST CONTROLLER:
    /// This is a perfect example of the service layer pattern.
    /// The entire controller body is two lines: call the service, return the result.
    /// All five SQL COUNT queries live in DashboardService — not here.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>GET /api/dashboard — returns live stats as JSON.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(stats);
        }
    }
}
