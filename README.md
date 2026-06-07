# GLMS – Global Logistics Management System
### Part 3 Submission – SOA, Docker & Automated Integration Testing
**ASP.NET Core Web API · ASP.NET Core MVC · EF Core · SQL Server · Docker · xUnit**

---

## Overview

Part 3 evolves the Part 2 monolithic prototype into a **Service-Oriented Architecture (SOA)**. The single MVC application has been split into three independently deployable components that communicate over HTTP and run together via Docker Compose.

| Component | Technology | Purpose |
|-----------|-----------|---------|
| `GLMS.Api` | ASP.NET Core Web API | Business logic, database access, REST endpoints |
| `GLMS.Web` | ASP.NET Core MVC | Presentation layer — calls the API via HttpClient |
| `sql-server-db` | MS SQL Server 2022 | Persistent data store (API only — Web has no DB access) |

All three containers are orchestrated by a single `docker-compose.yml` and communicate over an internal Docker bridge network.

---

## Quick Start (Docker)

> Requires: Docker Desktop running

```bash
docker compose up --build
```

| URL | What it is |
|-----|-----------|
| `http://localhost:5000` | GLMS MVC Frontend |
| `http://localhost:5001/swagger` | API Swagger UI |

**Login:** `admin` / `Admin@123`

To stop:
```bash
docker compose down
```

---

## Run Locally (Without Docker)

### Requirements
- Visual Studio 2022
- .NET 8 SDK
- SQL Server LocalDB

### 1. Start the API

```
cd GLMS.Api
dotnet run
```

API runs at `http://localhost:5001`. Swagger available at `/swagger`.

### 2. Start the Web App

```
cd GLMS.Web
dotnet run
```

Web runs at `http://localhost:5000`. Reads `ApiBaseUrl` from `appsettings.Development.json` (set to `http://localhost:5001`).

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Docker Network: glms-network          │
│                                                         │
│  ┌──────────────┐   HTTP    ┌──────────────┐            │
│  │  GLMS.Web    │ ────────► │  GLMS.Api    │            │
│  │  (MVC)       │           │  (Web API)   │            │
│  │  :5000       │           │  :5001       │            │
│  └──────────────┘           └──────┬───────┘            │
│                                    │ SQL                 │
│                              ┌─────▼──────┐             │
│                              │ SQL Server │             │
│                              │  :1433     │             │
│                              └────────────┘             │
└─────────────────────────────────────────────────────────┘
```

**Key architectural decisions:**
- `GLMS.Web` has **zero database dependency** — no DbContext, no EF Core, no connection string
- All business logic and DB access lives in `GLMS.Api`
- The Web frontend communicates with the API exclusively via `ApiService` (typed `HttpClient`)
- JWT authentication is issued by the API and forwarded by the Web on every request
- Design patterns (Factory, Strategy, Observer) were moved to `GLMS.Api`

---

## REST API Endpoints

All endpoints (except `/api/auth/login`) require a `Bearer` JWT token.

| Verb | Endpoint | Description |
|------|----------|-------------|
| POST | `/api/auth/login` | Returns JWT token |
| GET | `/api/clients` | List all clients |
| GET | `/api/clients/{id}` | Get client by ID |
| POST | `/api/clients` | Create client — returns 201 |
| PUT | `/api/clients/{id}` | Update client — returns 204 |
| DELETE | `/api/clients/{id}` | Delete client — returns 204 or 409 |
| GET | `/api/contracts` | List contracts (optional filters: status, startFrom, startTo) |
| GET | `/api/contracts/{id}` | Get contract with client and service requests |
| POST | `/api/contracts` | Create contract (multipart, supports PDF) — returns 201 |
| PUT | `/api/contracts/{id}` | Update contract — returns 204 |
| PATCH | `/api/contracts/{id}/status` | Update status only — fires Observer Pattern |
| DELETE | `/api/contracts/{id}` | Delete contract — returns 204 |
| GET | `/api/contracts/{id}/download` | Download signed agreement PDF |
| GET | `/api/servicerequests` | List all service requests |
| GET | `/api/servicerequests/{id}` | Get service request by ID |
| POST | `/api/servicerequests` | Create service request — Strategy Pattern applies CostZAR |
| PATCH | `/api/servicerequests/{id}/status` | Update status |
| DELETE | `/api/servicerequests/{id}` | Delete service request |
| GET | `/api/servicerequests/rate` | Current USD→ZAR exchange rate |
| GET | `/api/dashboard` | Aggregated stats (totalClients, totalContracts, etc.) |

---

## Automated Integration Tests

Tests are in `GLMS.Tests/`. Run them with:

```bash
dotnet test GLMS.Tests/GLMS.Tests.csproj --verbosity normal
```

Tests use `WebApplicationFactory<Program>` with an in-memory database — **no SQL Server required**.

### ApiIntegrationTests.cs (13 tests)

| Test | What it verifies |
|------|-----------------|
| `Get_WithoutToken_Returns401Unauthorized` (x4) | All protected endpoints reject unauthenticated requests |
| `Login_WithValidCredentials_Returns200AndToken` | Login returns 200 and a non-empty JWT token |
| `Login_WithInvalidCredentials_Returns401` (x3) | Wrong credentials return 401 |
| `GetContracts_WithValidToken_Returns200AndJsonArray` | Authenticated GET returns 200 + JSON array |
| `GetClients_WithValidToken_Returns200` | Authenticated GET /api/clients returns 200 |
| `GetServiceRequests_WithValidToken_Returns200` | Authenticated GET /api/servicerequests returns 200 |
| `GetDashboard_WithValidToken_Returns200WithStats` | Dashboard returns all expected JSON fields |
| `GetRate_WithValidToken_ReturnsPositiveRate` | Exchange rate endpoint returns a positive number |

### DataIntegrityTests.cs (8 tests)

| Test | What it verifies |
|------|-----------------|
| `CreateClient_ThenRead_DataMatchesExactly` | All fields survive the Create→Read round-trip |
| `CreateContract_ThenRead_DataMatchesExactly` | FK integrity + Factory Pattern field integrity |
| `CreateServiceRequest_ThenRead_DataMatchesAndStrategyRan` | Strategy Pattern proof: CostZAR > CostUSD |
| `CreateTwoClients_IDsAreDistinct` | PK auto-increment assigns unique IDs |
| `CreateClient_WithMissingRequiredFields_Returns400` | Validation rejects invalid data with 400 |
| `GetClient_NonExistentId_Returns404` | Missing records return 404, not 200 with null |
| `CreateClient_ThenDelete_ThenRead_Returns404` | DELETE actually removes the record |
| `CreateClient_ThenUpdate_ThenRead_ReturnsUpdatedValues` | PUT persists new values correctly |

---

## Project Structure

```
GLMS_Part2/
├── docker-compose.yml           # Orchestrates all 3 containers
├── Dockerfile.api               # Multi-stage build for GLMS.Api
├── Dockerfile.web               # Multi-stage build for GLMS.Web
├── .dockerignore                # Excludes obj/ bin/ to prevent NuGet path issues
├── GLMS.sln
│
├── GLMS.Api/                    # Web API — business logic + DB
│   ├── Controllers/             # Thin controllers: receive, delegate, respond
│   ├── Services/                # Business logic (IClientService, IContractService, etc.)
│   ├── Patterns/
│   │   ├── Factory/             # IContractFactory, Local, International, Resolver
│   │   ├── Strategy/            # ICurrencyConversionStrategy, UsdToZarStrategy
│   │   └── Observer/            # IStatusObserver, Subject, ServiceRequestObserver
│   ├── Data/                    # ApplicationDbContext (EF Core)
│   └── Program.cs               # DI, JWT, Swagger, EF Core setup
│
├── GLMS.Web/                    # MVC Frontend — presentation only
│   ├── Controllers/             # Call ApiService — zero DB code
│   ├── Services/
│   │   └── ApiService.cs        # Typed HttpClient wrapping all API calls
│   ├── Views/                   # Razor .cshtml views
│   └── Program.cs               # HttpClient, Session — no DbContext
│
├── GLMS.Shared/                 # Shared models used by both API and Web
│   └── Models/                  # Client, Contract, ServiceRequest, Enums
│
└── GLMS.Tests/                  # Integration tests
    ├── ApiIntegrationTests.cs   # 13 routing/auth/response tests
    ├── DataIntegrityTests.cs    # 8 create-then-read data integrity tests
    ├── CurrencyCalculationTests.cs
    └── FileValidationTests.cs
```

---

## CI/CD — GitHub Actions

Every push triggers the workflow in `.github/workflows/dotnet.yml`:

1. Checkout code
2. Install .NET 8 SDK
3. Restore NuGet packages
4. Build solution
5. Run all tests — deployment is blocked if any test fails

---

## AI Assistance Declaration

AI (Claude) was used **only for**:

- Generating boilerplate code structure
- Explaining design pattern implementation
- Debugging and error explanation
- Helping structure documentation like this file

All code was written, understood, and is owned by me. The code, documentation, and all artefacts in this submission were developed by me.

---

## Developer

**Tayler Usmar | ST10445063**
PROG7311 — Part 3
