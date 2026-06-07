using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GLMS.Api.Controllers
{
    /// <summary>
    /// Login request DTO — accepted by the POST /api/auth/login endpoint.
    /// Using a C# record for immutability and concise syntax.
    /// </summary>
    public record LoginRequest(string Username, string Password);

    /// <summary>
    /// Handles authentication for the GLMS API.
    ///
    /// JWT (JSON Web Token) overview:
    ///   A JWT is a signed, compact token that encodes Claims (user identity + roles).
    ///   The API signs it with a secret key; the client sends it back in every
    ///   subsequent request as "Authorization: Bearer {token}".
    ///   Because it is signed, the API can verify it without a database lookup.
    ///
    /// Flow:
    ///   1. Client POSTs credentials to /api/auth/login.
    ///   2. API validates credentials (here: hardcoded admin/Admin@123 for demo).
    ///   3. API returns a signed JWT with an 8-hour expiry.
    ///   4. Client stores the token (e.g., sessionStorage) and sends it with all requests.
    ///   5. [Authorize] attributes on other controllers reject requests without a valid token.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Authenticates a user and returns a signed JWT token.
        /// Credentials: username = "admin", password = "Admin123!"
        /// </summary>
        /// <param name="request">Login credentials (Username and Password).</param>
        /// <returns>
        /// 200 OK with { token, expiresAt, username } if credentials are valid.
        /// 401 Unauthorized with { message } if credentials are invalid.
        /// </returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simple credential check — in production this would query the Users table
            // and verify a hashed password (e.g., using ASP.NET Core Identity + BCrypt).
            if (request.Username != "admin" || request.Password != "Admin123!")
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // ---------------------------------------------------------------
            // STEP 1: Read JWT configuration from appsettings.json
            // ---------------------------------------------------------------
            var key      = _configuration["Jwt:Key"]      ?? throw new InvalidOperationException("JWT Key not configured.");
            var issuer   = _configuration["Jwt:Issuer"]   ?? throw new InvalidOperationException("JWT Issuer not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");

            // ---------------------------------------------------------------
            // STEP 2: Build the Claims (what the token says about the user)
            // Claims are key-value pairs embedded inside the JWT payload.
            // NameIdentifier = user ID, Name = username, Role = role for [Authorize(Roles=...)]
            // ---------------------------------------------------------------
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name,           "admin"),
                new Claim(ClaimTypes.Role,           "Administrator")
            };

            // ---------------------------------------------------------------
            // STEP 3: Create the signing key from the secret string
            // SymmetricSecurityKey uses the same key to sign and verify.
            // The key string is encoded to bytes (UTF-8) for the HMAC algorithm.
            // ---------------------------------------------------------------
            var signingKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // ---------------------------------------------------------------
            // STEP 4: Build the JWT token descriptor and create the token
            // ---------------------------------------------------------------
            var expires = DateTime.UtcNow.AddHours(8);

            var tokenDescriptor = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            expires,
                signingCredentials: signingCredentials
            );

            // ---------------------------------------------------------------
            // STEP 5: Serialise the token to a string and return it
            // The client sends this string in every request:
            //   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
            // ---------------------------------------------------------------
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return Ok(new
            {
                token     = tokenString,
                expiresAt = expires,
                username  = "admin"
            });
        }
    }
}
