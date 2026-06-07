using GLMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// PART 3 CHANGE: Database is GONE from GLMS.Web.
// GLMS.Web is now a pure MVC frontend — it talks to GLMS.Api via HTTP.
// ApplicationDbContext and EF Core are no longer registered here.
// =========================================================================

// =========================================================================
// 1. MVC
// =========================================================================
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

// =========================================================================
// 2. SESSION — stores the JWT token between requests.
//    When the user logs in, the token is saved to the session.
//    ApiService reads from the session on every API call.
// =========================================================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout    = TimeSpan.FromHours(8); // Match JWT expiry
    options.Cookie.HttpOnly  = true;   // JS cannot read the cookie (XSS protection)
    options.Cookie.IsEssential = true; // Required even without cookie consent
});

builder.Services.AddHttpContextAccessor(); // Needed by ApiService to read the session

// =========================================================================
// 3. API SERVICE — typed HttpClient that wraps all calls to GLMS.Api.
//    The base URL is read from appsettings.json ("ApiBaseUrl").
//    In Docker: http://glms-backend-api:8080
//    In local dev: http://localhost:5001
// =========================================================================
builder.Services.AddHttpClient<ApiService>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? "http://localhost:5001";
    client.BaseAddress = new Uri(apiBaseUrl);
});

// =========================================================================
// 4. BUILD & CONFIGURE PIPELINE
// =========================================================================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session middleware must come BEFORE Authorization and MapControllerRoute
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
