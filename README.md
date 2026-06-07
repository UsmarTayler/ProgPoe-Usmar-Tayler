# GLMS – Global Logistics Management System
### Part 2 Submission – Core Prototype & Unit Testing
**ASP.NET Core MVC · EF Core · SQL Server · xUnit · External API**

---

## 📌 Overview

The **Global Logistics Management System (GLMS)** is a monolithic ASP.NET Core MVC web application built for TechMove Logistics. It serves as the functional prototype for the enterprise system designed in Part 1, translating the architecture and UML diagrams directly into working C# code.

The system includes:

- Full client, contract, and service request management
- All three Gang of Four design patterns from Part 1 (Factory, Strategy, Observer)
- Live USD → ZAR currency conversion via external API
- PDF signed agreement upload and download
- LINQ-based contract search and filter
- Workflow business rules (status validation)
- 20 xUnit unit tests covering currency math and file validation

---

## 🚀 How to Run

### Requirements
- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)
- EF Core Tools

---

### 1. Database Setup

Open **Package Manager Console** (Tools → NuGet Package Manager → PMC):

```
Add-Migration InitialCreate
Update-Database
```

This will create:

- Clients table
- Contracts table (with SignedAgreementPath for PDF uploads)
- ServiceRequests table (with CostUSD, CostZAR, ExchangeRateUsed)

---

### 2. Run the Application

1. Open **GLMS.sln** in Visual Studio 2022
2. Set **GLMS.Web** as the startup project
3. Press **F5**
4. The app opens at `https://localhost:[port]/`

---

## 📋 Features (Rubric Aligned)

## 1️⃣ Database Architecture – EF Core (10 Marks)

- Fully normalised schema with 3 related tables
- Fluent API configuration (relationships, decimal precision, enum-as-string)
- One-to-Many: Client → Contracts → ServiceRequests
- Connection string in `appsettings.json` — not hardcoded
- Foreign key constraints with Restrict and Cascade delete rules

---

## 2️⃣ Design Pattern Implementation – From Part 1 UML (20 Marks)

### Factory Method
- `IContractFactory`, `LocalContractFactory`, `InternationalContractFactory`
- `ContractFactoryResolver` selects the correct factory based on user selection
- Controller never creates contracts directly — the factory handles it

### Strategy Pattern
- `ICurrencyStrategy`, `UsdToZarStrategy`, `EurToZarStrategy`
- `FinancialProcessor` holds and delegates to the active strategy
- Swappable at runtime — adding a new currency requires zero existing code changes

### Observer Pattern
- `ContractStatusSubject` maintains a list of observers
- `ServiceRequestObserver` auto-cancels Pending requests when contract Expires or goes OnHold
- `FinanceObserver` logs a finance audit alert on every status change
- Both observers fire automatically from one line in the controller

---

## 3️⃣ Workflow & Validation Logic 
- Service Requests **cannot** be created if the parent contract is `Expired` or `OnHold`
- Validated at UI level (only eligible contracts appear in dropdown)
- Validated at server level (POST action re-checks status even if UI is bypassed)
- Observer Pattern retroactively cancels Pending requests when a contract's status changes

---

## 4️⃣ File Handling – PDF Uploads 

- PDF validation checks **both** file extension and MIME content type
- Files saved with UUID prefix to prevent overwrite collisions
- Stored in `wwwroot/uploads/` (simulated file server)
- Download link available on the Contracts index and details pages

---

## 5️⃣ External API Integration – Currency Conversion 

- Fetches live USD → ZAR rate from `https://open.er-api.com/v6/latest/USD`
- No API key required — free tier
- Rate displayed on ServiceRequest creation page
- ZAR field auto-calculates as user types the USD amount (JavaScript)
- Rate re-fetched server-side on POST for security
- Fallback rate of 18.75 used if API is unavailable
- Uses `async/await` with `HttpClient` (LU4 — Optimising Application Performance)

---

## 🧪 Unit Tests 

Unit tests are in the **GLMS.Tests** project.

To run: Open **Test Explorer** → **Run All Tests**

All 20 tests should pass (green).

### CurrencyCalculationTests (9 tests)
- Correct USD → ZAR conversion math
- Decimal rounding to 2 places
- Zero amount edge case
- Invalid rate throws `ArgumentException`
- Strategy swap at runtime
- Parameterised `[Theory]` with `[InlineData]`

### FileValidationTests (11 tests)
- PDF accepted
- `.exe` rejected
- `.docx` and `.jpg` rejected
- MIME type spoofing detected and rejected
- Null and empty filename handling
- Case-insensitive extension check (`.PDF` accepted)
- Parameterised `[Theory]` with 7 file types

---

## 🗂️ Project Structure

```
GLMS_Part2/
├── GLMS.sln
├── GLMS.Web/
│   ├── Controllers/         # ClientsController, ContractsController, ServiceRequestsController
│   ├── Data/                # ApplicationDbContext (EF Core)
│   ├── Models/              # Client, Contract, ServiceRequest, Enums
│   ├── Patterns/
│   │   ├── Factory/         # IContractFactory, Local, International, Resolver
│   │   ├── Strategy/        # ICurrencyStrategy, UsdToZar, EurToZar, FinancialProcessor
│   │   └── Observer/        # IStatusObserver, Subject, ServiceRequestObserver, FinanceObserver
│   ├── Services/            # CurrencyService (API), FileValidationService (PDF)
│   ├── Views/               # Razor .cshtml views for all entities
│   ├── appsettings.json     # Connection string (not hardcoded)
│   └── Program.cs           # DI registration, middleware, auto-migration
└── GLMS.Tests/
    ├── CurrencyCalculationTests.cs   # 9 unit tests
    └── FileValidationTests.cs        # 11 unit tests
```

---

## 🤖 AI Assistance Declaration

AI (Claude) was used **only for**:

- Generating boilerplate code structure
- Explaining design pattern implementation
- Debugging and error explanation
- Helping structure documentation like this file

**Where AI tools were used for guidance or explanation, this has been noted. All code was written, understood, and is owned by me, The code, documentation, and all artefacts in this submission were developed by me.**

---

## 👤 Developer

**Tayler Usmar | ST10445063**
PROG7311 
