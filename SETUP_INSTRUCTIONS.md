# GLMS Part 2 — Setup & Run Instructions
## PROG7311 | Student: ST10445063

---

## Prerequisites
- Visual Studio 2022 (Community or higher)
- .NET 8.0 SDK
- SQL Server LocalDB (included with Visual Studio)

---

## Step 1: Open the Solution
Open `GLMS.sln` in Visual Studio 2022.

---

## Step 2: Create the Database (EF Core Migration)

Open the **Package Manager Console** (Tools → NuGet Package Manager → PMC)
and make sure the **Default project** is set to `GLMS.Web`, then run:

```powershell
# 1. Create the initial migration
Add-Migration InitialCreate

# 2. Apply migration — creates the GLMS_DB database
Update-Database
```

> The database is auto-created at startup too (via `db.Database.Migrate()` in Program.cs),
> but running the migration manually generates the migration script files for submission.

---

## Step 3: Run the Application

Press **F5** or click the green **Run** button in Visual Studio.
The app will open at `https://localhost:PORT/`

---

## Step 4: Run Unit Tests

Open the **Test Explorer** (Test → Test Explorer) and click **Run All Tests**.

You should see **all tests passing (green)** including:
- `CurrencyCalculationTests` (9 tests)
- `FileValidationTests` (11 tests)

Take a screenshot of the Test Explorer with all green tests for submission.

---

## Architecture Overview

### Design Patterns (from Part 1 UML)

| Pattern | Files | Purpose |
|---------|-------|---------|
| Factory Method | `Patterns/Factory/` | Creates Local vs International contracts |
| Strategy | `Patterns/Strategy/` | USD → ZAR currency conversion |
| Observer | `Patterns/Observer/` | Cancels service requests when contract expires |

### Key Features

| Feature | Where to find it |
|---------|-----------------|
| EF Core + SQL Server | `Data/ApplicationDbContext.cs` |
| LINQ Search/Filter | `Controllers/ContractsController.cs` → Index() |
| PDF Upload | `Controllers/ContractsController.cs` → Create/Edit |
| Currency API | `Services/CurrencyService.cs` → GetUsdToZarRateAsync() |
| Workflow Rule | `Controllers/ServiceRequestsController.cs` → Create() |
| Observer fires | `Controllers/ContractsController.cs` → Edit() |
| Unit Tests | `GLMS.Tests/` |

---

## Database Migration Script (for submission)

After running `Add-Migration InitialCreate`, a file will appear at:
`GLMS.Web/Migrations/XXXXXXXXXX_InitialCreate.cs`

Include this file in your GitHub repo submission.

---

## External API Used
- **ExchangeRate-API (Free):** `https://open.er-api.com/v6/latest/USD`
- No API key required for the free tier.
- Returns JSON: `{ "rates": { "ZAR": 18.75 } }`
- If the API is down, a fallback rate of 18.75 is used automatically.
