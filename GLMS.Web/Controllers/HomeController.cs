using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The Home dashboard controller for the GLMS MVC frontend.
    ///
    /// WHAT CHANGED IN PART 3 (SOA Refactoring):
    /// Previously: injected ApplicationDbContext and ran LINQ queries directly on the DB.
    /// Now: injects ApiService and calls GET /api/dashboard on GLMS.Api.
    /// The API runs all the DB queries and returns a JSON stats object.
    /// This controller just receives the numbers and passes them to the Razor view.
    ///
    /// PRESENTATION POINT — SOA Separation:
    /// The MVC app is now the "Presentation Layer" only.
    /// Business logic + database access lives entirely in GLMS.Api ("Service Layer").
    /// They communicate via HTTP/JSON — this is the Service-Oriented Architecture.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApiService _api;

        // PRESENTATION POINT: Constructor Injection — ApiService is registered in Program.cs
        // using builder.Services.AddHttpClient<ApiService>().
        public HomeController(ApiService api)
        {
            _api = api;
        }

        /// <summary>
        /// GET: / — Loads the Dashboard with live stats.
        /// In Part 2 this ran five SQL queries directly.
        /// In Part 3 it makes ONE HTTP call to the API which runs all five queries.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Guard: redirect to login if session has no JWT token
            if (HttpContext.Session.GetString("JwtToken") == null)
                return RedirectToAction("Login", "Account");

            // Call GET /api/dashboard (GLMS.Api queries the DB and returns JSON)
            var stats = await _api.GetDashboardStatsAsync();

            // Pass stats to the Razor view via ViewBag (same names as Part 2)
            ViewBag.TotalClients         = stats?.TotalClients         ?? 0;
            ViewBag.TotalContracts       = stats?.TotalContracts       ?? 0;
            ViewBag.ActiveContracts      = stats?.ActiveContracts      ?? 0;
            ViewBag.TotalServiceRequests = stats?.TotalServiceRequests ?? 0;
            ViewBag.PendingRequests      = stats?.PendingRequests      ?? 0;
            ViewBag.TotalRevenue         = stats?.TotalRevenue         ?? 0m;
            ViewBag.Username             = HttpContext.Session.GetString("Username") ?? "admin";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
