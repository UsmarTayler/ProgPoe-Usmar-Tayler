using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Web.Controllers
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// Handles user authentication for the GLMS MVC frontend.
    ///
    /// WHAT I DID (Part 3 — JWT Auth):
    /// I added this controller to implement authentication between the MVC frontend
    /// and the GLMS.Api backend. The flow works like this:
    ///
    ///   1. User navigates to /Account/Login (GET)
    ///   2. User types "admin" / "Admin@123" and submits the form
    ///   3. AccountController.Login (POST) calls ApiService.LoginAsync()
    ///   4. ApiService sends credentials to POST /api/auth/login on GLMS.Api
    ///   5. GLMS.Api validates the credentials and returns a signed JWT token
    ///   6. AccountController stores the JWT in the session ("JwtToken" key)
    ///   7. User is redirected to the Home page
    ///   8. Every subsequent API call from ApiService reads the session token
    ///      and adds "Authorization: Bearer {token}" to the HTTP request header
    ///
    /// WHY SESSION?
    /// Sessions store data server-side. The client only holds a session cookie
    /// (a random ID). This is more secure than storing the JWT in localStorage
    /// (which is vulnerable to XSS attacks).
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
        }

        // =====================================================================
        // LOGIN — GET
        // =====================================================================

        /// <summary>Shows the login form. If already logged in, redirect to home.</summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Already logged in — don't show the login form again
            if (HttpContext.Session.GetString("JwtToken") != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // =====================================================================
        // LOGIN — POST
        // =====================================================================

        /// <summary>
        /// Submits credentials to GLMS.Api, stores the JWT in session on success.
        /// Credentials: admin / Admin@123
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            // Call GLMS.Api → POST /api/auth/login
            var token = await _api.LoginAsync(username, password);

            if (string.IsNullOrEmpty(token))
            {
                // API returned 401 — wrong credentials
                ModelState.AddModelError("", "Invalid username or password. Try: admin / Admin@123");
                return View();
            }

            // Store the JWT in the server-side session
            HttpContext.Session.SetString("JwtToken",  token);
            HttpContext.Session.SetString("Username",  username);

            TempData["Success"] = $"Welcome back, {username}! You are now logged in.";

            // Redirect to the page the user was trying to access, or home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // =====================================================================
        // LOGOUT
        // =====================================================================

        /// <summary>Clears the session (removes the JWT token) and redirects to login.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction(nameof(Login));
        }
    }
}
