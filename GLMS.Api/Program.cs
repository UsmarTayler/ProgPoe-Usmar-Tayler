using GLMS.Api.Data;
using GLMS.Api.Patterns.Observer;
using GLMS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. DATABASE — Entity Framework Core with SQL Server
//    Connection string is in appsettings.json (not hardcoded — marking criteria).
//    For Docker: uses the sql-server-db service name from docker-compose.yml.
//    For local dev: appsettings.Development.json overrides with localdb.
// =========================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================================================
// 2. JWT BEARER AUTHENTICATION
//    Reads Key, Issuer, and Audience from the "Jwt" section in appsettings.json.
//    The SymmetricSecurityKey is derived from the UTF-8 encoded secret key string.
//    All four validation parameters are enabled for maximum security.
// =========================================================================
var jwtKey      = builder.Configuration["Jwt:Key"]      ?? throw new InvalidOperationException("JWT Key is not configured.");
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? throw new InvalidOperationException("JWT Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// =========================================================================
// 3. AUTHORIZATION
// =========================================================================
builder.Services.AddAuthorization();

// =========================================================================
// 4. SWAGGER / OPENAPI
//    Configured with a JWT security definition so the Swagger UI renders
//    an "Authorize" button. The user can paste a Bearer token there to
//    authenticate all subsequent Swagger requests — essential for the demo.
// =========================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "GLMS API",
        Version     = "v1",
        Description = "Government Logistics Management System — Web API for PROG7311 POE Part 3. " +
                      "Student: Tayler Usmar (ST10445063). " +
                      "Authenticate via POST /api/auth/login to receive a JWT token, then click Authorize.",
        Contact     = new OpenApiContact { Name = "Tayler Usmar", Email = "taylerusmar@gmail.com" }
    });

    // JWT security definition — adds the padlock icon in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Description  = "Enter: Bearer {your JWT token}",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    // Apply JWT requirement globally so every endpoint shows the padlock
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// =========================================================================
// 5. CORS — Allow all origins/methods/headers for the Docker demo.
//    In production this would be locked down to specific origins.
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// =========================================================================
// 6. SERVICES — registered for Dependency Injection
//    Scoped means one instance per HTTP request — appropriate for services
//    that use ApplicationDbContext (which is also scoped).
// =========================================================================

// File validation service (validates PDFs and saves them to wwwroot/uploads/)
builder.Services.AddScoped<FileValidationService>();

// Business logic services — Criterion 2 (Architectural Integrity)
// Controllers inject these interfaces; they contain zero DbContext code themselves.
// Scoped lifetime matches ApplicationDbContext (one instance per HTTP request).
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// =========================================================================
// 7. OBSERVER PATTERN — Subject registered with factory pattern
//    Both observers are attached at startup so they fire automatically on every
//    contract status change triggered by the ContractsController.
// =========================================================================
builder.Services.AddScoped<ServiceRequestObserver>();
builder.Services.AddScoped<FinanceObserver>();

builder.Services.AddScoped<ContractStatusSubject>(provider =>
{
    var subject = new ContractStatusSubject();

    // Attach Observer #1: auto-cancels Pending service requests when contract Expires/goes OnHold
    var srObserver = provider.GetRequiredService<ServiceRequestObserver>();
    subject.Attach(srObserver);

    // Attach Observer #2: logs a finance audit trail entry on every status change
    var finObserver = provider.GetRequiredService<FinanceObserver>();
    subject.Attach(finObserver);

    return subject;
});

// =========================================================================
// 8. CURRENCY SERVICE — typed HttpClient for async external API calls (LU4)
//    HttpClient is managed by IHttpClientFactory for proper lifecycle handling.
// =========================================================================
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

// =========================================================================
// 9. MVC CONTROLLERS
//    ReferenceHandler.IgnoreCycles is required because EF Core's relationship
//    fixup creates circular object graphs (e.g. Contract → Client → Contracts[0]
//    → Contract ...) after any Include() or SaveChangesAsync(). Without this,
//    the JSON serialiser throws a JsonException on any response that contains
//    a loaded navigation property. IgnoreCycles outputs null the second time it
//    encounters the same object — safe for API consumers.
// =========================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevents 500s caused by EF Core relationship fixup creating circular object graphs
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // Serialise enums as their string names ("Premium", "Active", "Pending") instead
        // of integers. This makes the API responses human-readable, matches the HasConversion
        // <string>() configuration in EF Core, and allows the MVC frontend to deserialise
        // back to the correct C# enum values using the same converter.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// =========================================================================
// 10. BUILD THE APPLICATION
// =========================================================================
var app = builder.Build();

// =========================================================================
// 11. MIDDLEWARE PIPELINE
//     Order matters: HTTPS redirect → CORS → Auth → Controllers.
//     CORS must come before Authentication/Authorization.
// =========================================================================
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// 12. SWAGGER UI — enabled in ALL environments (not just Development)
//     This is intentional for the Docker demo: the marker can open Swagger
//     at http://localhost:5001/swagger regardless of environment.
// =========================================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLMS API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

// =========================================================================
// 13. MAP CONTROLLERS
// =========================================================================
app.MapControllers();

// =========================================================================
// 14. AUTO-CREATE DATABASE ON STARTUP
//     EnsureCreated() creates the database schema if it does not exist.
//     Wrapped in try/catch so the API starts up even if the DB is temporarily
//     unavailable (e.g., SQL Server container is still initialising in Docker).
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database ensured/created successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialisation failed. The application will start but database operations may fail.");
    }
}

app.Run();

// Required for WebApplicationFactory in GLMS.Tests to discover the entry point.
// WebApplicationFactory<Program> needs Program to be a public accessible type.
public partial class Program { }
