# MplDbApi

A .NET 10 read data microservice that exposes material-market data, property definitions, source and unit references, and role-based filter configuration. Part of the **Mpl-User-Service** mono-repo.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [API Reference](#api-reference)
  - [Delivery Types](#delivery-types)
  - [Sources](#sources)
  - [Units](#units)
  - [Material Groups](#material-groups)
  - [Material Properties](#material-properties)
  - [Material Sources (Materials)](#material-sources-materials)
  - [Material Values](#material-values)
  - [Filter Configuration](#filter-configuration)
  - [Cache Management](#cache-management)
- [Running the Service](#running-the-service)
- [Database Migrations](#database-migrations)
- [Tests](#tests)
  - [Test Structure](#test-structure)
  - [Running Tests](#running-tests)
  - [Service Tests](#service-tests)
  - [Route Tests](#route-tests)
  - [Code Coverage](#code-coverage)

---

## Overview

MplDbApi provides:

- **Material data** — materials with their sources, delivery types, groups, units, and target markets, with latest-price and change-percent computation
- **Market metrics** — time-series values per material with date-range queries, weekly/monthly/quarterly/yearly averages, and overview table aggregation
- **Reference data** — delivery types, sources, units, material groups, and property definitions
- **Role-based data filtering** — per-role exclusion lists (materials, groups, sources, units, properties) stored in a separate SQLite database and applied transparently to every data query
- **In-memory caching** — 2-hour cache on heavy material-list queries, with an admin flush endpoint

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Minimal APIs) |
| ORM | Entity Framework Core 10 |
| Main database | PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Filter database | SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Authentication | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Caching | `Microsoft.Extensions.Caching.Memory` |
| Target Runtime | .NET 10 |

---

## Project Structure

```
db/
├── src/
│   └── MplDbApi/
│       ├── Program.cs                      # DI setup, middleware pipeline, route registration
│       ├── appsettings.json                # Base configuration
│       ├── appsettings.Development.json
│       ├── Data/
│       │   ├── BMplbaseContext.cs          # EF Core DbContext for main PostgreSQL database
│       │   └── FilterContext.cs            # EF Core DbContext for SQLite filter database
│       ├── Interfaces/                     # Service contracts
│       │   ├── IDeliveryTypeService.cs
│       │   ├── IMaterialGroupService.cs
│       │   ├── IMaterialPropService.cs
│       │   ├── IMaterialSourceService.cs
│       │   ├── IMaterialValueService.cs
│       │   ├── ISourceService.cs
│       │   └── IUnitService.cs
│       ├── Migrations/                     # EF Core migration history (FilterContext)
│       ├── Models/
│       │   ├── DeliveryType.cs
│       │   ├── Material.cs
│       │   ├── MaterialGroup.cs
│       │   ├── MaterialProperty.cs
│       │   ├── MaterialSource.cs
│       │   ├── MaterialValue.cs
│       │   ├── Property.cs
│       │   ├── Source.cs
│       │   ├── Unit.cs
│       │   ├── Dtos/                       # Request / response DTOs
│       │   └── Filters/
│       │       └── Filter.cs               # DataFilter entity
│       ├── Routes/
│       │   ├── DeliveryTypeRoute.cs        # /deliverytypes
│       │   ├── SourceRoutes.cs             # /sources
│       │   ├── UnitRoutes.cs               # /units
│       │   ├── MaterialGroupRoutes.cs      # /materialgroups
│       │   ├── MaterialPropertyRoutes.cs   # /material/{id}/properties  /properties/dropdown
│       │   ├── MaterialSourceRoute.cs      # /materials  /materials/{id}  /materials/bygroup/{id}
│       │   ├── MaterialValueRoute.cs       # /materialvalues/{id}  /materialvalues/overview  /materialvalues/daterange
│       │   ├── FilterConfigRoutes.cs       # /filter-config/filter  /filter-config/filters
│       │   └── CacheManagementRoutes.cs    # /cache/clear
│       ├── Services/
│       │   ├── DeliveryTypeService.cs
│       │   ├── SourceService.cs
│       │   ├── UnitService.cs
│       │   ├── MaterialGroupService.cs
│       │   ├── MaterialPropService.cs
│       │   ├── MaterialSourceService.cs
│       │   ├── MaterialValueService.cs
│       │   ├── FilterService.cs
│       │   └── CacheManagementService.cs
│       └── Utils/
│           ├── DatabaseInitializer.cs
│           └── ValueChangeFormatter.cs     # Formats price-change percentage strings
└── tests/
    └── MplDbApi.Tests/
        ├── Services/                       # Unit tests for service classes
        └── Routes/                         # Integration tests for HTTP endpoints
```

---

## Configuration

All settings are in `appsettings.json` and can be overridden by environment variables.

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string for main data | _(required)_ |
| `ConnectionStrings:FilterConnection` | SQLite connection string for filter data | _(required)_ |
| `Jwt:Key` | HMAC-SHA256 signing key shared with `MplAuthService` | _(required)_ |
| `Jwt:Issuer` | JWT `iss` claim | `MplAuth` |
| `Jwt:Audience` | JWT `aud` claim | `MplService` |
| `Kestrel:EndPoints:Http:Url` | Bind address | `http://localhost:5201` |

---

## API Reference

All endpoints require an `Authorization: Bearer <token>` header. Endpoints under `/filter-config` additionally require the `Admin` role (`RequireAdmin` policy).

### Role extraction

Several endpoints derive an effective role from the authenticated user's claims:

```
extractedRole = (ClaimTypes.Role == "Admin") ? "Admin"
              : ClaimTypes("SubscriptionType") ?? "Free"
```

This extracted role is passed to services to apply the appropriate `DataFilter`.

---

### Delivery Types

#### `GET /deliverytypes`

Returns all delivery types.

**Responses**: `200 OK` with array of `DeliveryTypeDto` | `500`

---

#### `GET /deliverytypes/{id}`

Returns a single delivery type by ID.

**Responses**: `200 OK` with `DeliveryTypeDto` | `404 Not Found` | `500`

---

### Sources

#### `GET /sources`

Returns all data sources.

**Responses**: `200 OK` with array of `IdValuePair` | `500`

---

### Units

#### `GET /units`

Returns all measurement units.

**Responses**: `200 OK` with array of `IdValuePair` | `500`

---

### Material Groups

#### `GET /materialgroups`

Returns material groups visible to the authenticated user's effective role (groups in the role's filter are excluded).

**Responses**: `200 OK` with array of `MaterialGroupDto` | `500`

---

### Material Properties

#### `GET /material/{materialId}/properties`

Returns properties associated with a specific material source.

**Responses**: `200 OK` with array of `MaterialPropertyResp` | `500`

---

#### `GET /properties/dropdown`

Returns all property definitions for use in dropdowns.

**Responses**: `200 OK` with array of `IdValuePair` | `500`

---

### Material Sources (Materials)

#### `GET /materials`

Returns all materials visible to the authenticated user's effective role, including latest prices, change percentage, and available property IDs. Results are cached for 2 hours.

**Responses**: `200 OK` with array of `MaterialSourceResponseDto` | `500`

---

#### `GET /materials/{id}`

Returns a single material by its source ID.

**Responses**: `200 OK` with `MaterialSourceResponseDto` | `404 Not Found` | `500`

---

#### `GET /materials/bygroup/{groupId}`

Returns all materials belonging to a specific group, filtered by the user's effective role.

**Responses**: `200 OK` with array of `MaterialSourceResponseDto` | `404 Not Found` | `500`

---

### Material Values

#### `GET /materialvalues/{id}`

Returns a single material value record by ID. Returns `404` if the material is excluded by the user's filter.

**Responses**: `200 OK` with `MaterialValueResponseDto` | `404 Not Found` | `500`

---

#### `POST /materialvalues/daterange`

Returns time-series metrics for a single material over a date range, optionally including computed aggregates (weekly, monthly, quarterly, yearly averages).

**Request body** (`MaterialDateMetricReq`)
```json
{
  "materialId": 1,
  "propertyIds": [1, 2, 3],
  "startDate": "2025-01-01",
  "endDate": "2025-06-30",
  "aggregates": ["weekly", "monthly"]
}
```

**Responses**: `200 OK` with array of `MaterialDateMetrics` | `400 Bad Request` | `500`

---

#### `POST /materialvalues/overview`

Returns date-grouped metrics for multiple materials simultaneously.

**Request body**: array of `MaterialDateMetricReq`

**Responses**: `200 OK` with array of `DateGroupedMaterialValues` | `400 Bad Request` (empty array) | `500`

---

### Filter Configuration

All endpoints require the `Admin` role.

#### `POST /filter-config/filter`

Creates or updates a role-based data filter.

**Request body** (`FilterCreateReqDto`)
```json
{
  "affectedRole": "viewer",
  "groups": [2, 3],
  "sources": null,
  "units": null,
  "materialIds": [10, 11],
  "properties": null
}
```

**Responses**: `200 OK` | `400 Bad Request` (null body) | `500`

---

#### `GET /filter-config/filter/{role}`

Returns the filter for a specific role. Returns an empty `DataFilter` when no filter is configured for that role.

**Responses**: `200 OK` with `DataFilter` | `500`

---

#### `GET /filter-config/filters`

Returns all configured filters.

**Responses**: `200 OK` with array of `DataFilter` | `500`

---

### Cache Management

#### `POST /cache/clear`

Clears the entire in-memory cache. No authorization policy is applied to this endpoint beyond the standard JWT requirement.

**Responses**: `200 OK` with `{ "message": "Cache cleared successfully" }` | `500`

---

## Running the Service

### Prerequisites

- .NET 10 SDK
- PostgreSQL instance with material data
- The SQLite file for filter data will be created automatically on first run

### Steps

```bash
# 1. Configure connection strings and JWT key
# Edit appsettings.Development.json or set environment variables

# 2. Apply the filter database migration
cd src/MplDbApi
dotnet ef database update --context FilterContext

# 3. Run
dotnet run
# Service listens on http://localhost:5201 by default
```

---

## Database Migrations

The service has two EF Core contexts. Only `FilterContext` (SQLite) has managed migrations; `BMplbaseContext` (PostgreSQL) reflects a pre-existing database schema.

```bash
# Add a new migration for the filter database
dotnet ef migrations add <MigrationName> --context FilterContext --project src/MplDbApi

# Apply to the filter database
dotnet ef database update --context FilterContext --project src/MplDbApi

# Rollback to a specific migration
dotnet ef database update <MigrationName> --context FilterContext --project src/MplDbApi
```

---

## Tests

The test project lives in `tests/MplDbApi.Tests/` and targets .NET 10.

### Test Stack

| Package | Version | Purpose |
|---|---|---|
| xUnit | 2.9.2 | Test framework |
| Moq | 4.20.72 | Mocking |
| EF Core InMemory | 10.0.0 | In-memory database for isolation |
| Microsoft.AspNetCore.TestHost | 10.0.0 | In-process HTTP server for route tests |
| coverlet.collector | 6.0.2 | Code coverage |

### Test Structure

```
tests/MplDbApi.Tests/
├── Services/                           # Unit tests — no HTTP layer
│   ├── DeliveryTypeServiceTests.cs     # 5 tests
│   ├── SourceServiceTests.cs           # 3 tests
│   ├── UnitServiceTests.cs             # 3 tests
│   ├── FilterServiceTests.cs           # 9 tests
│   ├── MaterialGroupServiceTests.cs    # 5 tests
│   ├── MaterialPropServiceTests.cs     # 7 tests
│   ├── MaterialValueServiceTests.cs    # 5 tests
│   └── CacheManagementServiceTests.cs  # 4 tests
└── Routes/                             # Integration tests — real HTTP via TestHost
    ├── Helpers/
    │   ├── TestAuthHandler.cs          # Bypasses JWT auth; always returns Admin identity
    │   └── RouteTestHost.cs            # Shared host-builder utility
    ├── DeliveryTypeRoutesTests.cs      # 6 tests
    ├── SourceRoutesTests.cs            # 3 tests
    ├── UnitRoutesTests.cs              # 3 tests
    ├── MaterialGroupRoutesTests.cs     # 4 tests
    ├── MaterialPropertyRoutesTests.cs  # 5 tests
    ├── MaterialValueRoutesTests.cs     # 8 tests
    ├── MaterialSourceRoutesTests.cs    # 9 tests
    ├── FilterConfigRoutesTests.cs      # 7 tests
    └── CacheManagementRoutesTests.cs   # 2 tests
```

**Total: 88 tests**

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=normal"

# Run a specific test class
dotnet test --filter "ClassName=FilterServiceTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~GetAllMaterials_Returns200"
```

### Service Tests

Unit tests exercise each service class in isolation. EF Core's InMemory provider is used for both `BMplbaseContext` and `FilterContext`; services that depend on `FilterService` receive a real instance backed by an in-memory `FilterContext`.

**Key pattern**

```csharp
var options = new DbContextOptionsBuilder<BMplbaseContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())   // unique DB per test class
    .Options;

using var context = new BMplbaseContext(options);
var service = new DeliveryTypeService(context);
```

> Services that use `GroupBy` with non-aggregate projections (e.g., `MaterialValueService.GetMaterialMetricsByDateRange`) cannot be fully unit-tested against EF Core's InMemory provider because the provider cannot translate those queries. The filter-exclusion path is covered; the data-fetching path requires an integration test against a real database.

#### `DeliveryTypeServiceTests` (5 tests)
- `GetDeliveryTypesAsync` — full list, empty, correct DTO fields.
- `GetDeliveryTypeByIdAsync` — found, not found.

#### `SourceServiceTests` (3 tests)
- `GetSources` — full list, empty, correct DTO fields.

#### `UnitServiceTests` (3 tests)
- `GetUnits` — full list, empty, correct DTO fields.

#### `FilterServiceTests` (9 tests)
- `GetFilterByRole` — `null` role returns default filter, empty string returns default filter, known role returns its filter, unknown role returns empty `DataFilter`.
- `ModifyFilter` — creates new filter, updates existing filter, throws on empty `AffectedRole`.
- `GetAllFilters` — non-empty and empty list.

#### `MaterialGroupServiceTests` (5 tests)
- `GetMaterialGroupAsync` — all groups when no filter, empty, filtered groups are excluded, null groups in filter returns all, correct DTO fields.

#### `MaterialPropServiceTests` (7 tests)
- `GetMaterialProperties` — properties for a material, empty for unknown material, only properties for the requested material, multiple properties.
- `GetMaterialPropertiesForDropdown` — full list, empty, correct DTO fields.

#### `MaterialValueServiceTests` (5 tests)
- `GetMaterialValueById` — returns DTO, returns `null` when not found, returns `null` when material is in the role's filter, returns DTO when material is not in the filter.
- `GetMaterialMetricsByDateRange` — returns empty list when material is excluded by the role's filter.

#### `CacheManagementServiceTests` (4 tests)
- `ClearCache` — does not throw, removes existing entries, succeeds on empty cache, logs an `Information` message.

---

### Route Tests

Integration tests spin up a real `WebApplication` using `UseTestServer()`. All service interfaces are replaced with Moq mocks; `FilterService` and `CacheManagementService` use real instances backed by an in-memory `FilterContext`. Each test class builds its own isolated host — no shared state between classes.

**Key pattern**

```csharp
var host = await new RouteTestHost().BuildAsync(app => app.MapDeliveryTypeRoutes());

host.DeliveryTypeServiceMock
    .Setup(s => s.GetDeliveryTypesAsync())
    .ReturnsAsync([new DeliveryTypeDto(1, "EXW")]);

var response = await host.Client.GetAsync("/deliverytypes");
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```

**`TestAuthHandler`** (`Routes/Helpers/TestAuthHandler.cs`) is a custom `AuthenticationHandler` that always returns an authenticated `ClaimsPrincipal` with an `Admin` role claim, allowing all `RequireAuthorization`- and `RequireAdmin`-protected routes to be tested without a real JWT.

#### `DeliveryTypeRoutesTests` (6 tests)
- `GET /deliverytypes` — `200` with list, `200` empty, `500` on service exception.
- `GET /deliverytypes/{id}` — `200` found, `404` not found, `500` on service exception.

#### `SourceRoutesTests` (3 tests)
- `GET /sources` — `200` with list, `200` empty, `500` on service exception.

#### `UnitRoutesTests` (3 tests)
- `GET /units` — `200` with list, `200` empty, `500` on service exception.

#### `MaterialGroupRoutesTests` (4 tests)
- `GET /materialgroups` — `200` with list, `200` empty, verifies `Admin` role is extracted and passed to service, `500` on service exception.

#### `MaterialPropertyRoutesTests` (5 tests)
- `GET /material/{id}/properties` — `200` with list, `200` empty, `500`.
- `GET /properties/dropdown` — `200` with list, `500`.

#### `MaterialValueRoutesTests` (8 tests)
- `GET /materialvalues/{id}` — `200` found, `404` not found, `500`.
- `POST /materialvalues/overview` — `200` with data, `400` empty body, `500`.
- `POST /materialvalues/daterange` — `200` with data, `500`.

#### `MaterialSourceRoutesTests` (9 tests)
- `GET /materials` — `200` with list, verifies `Admin` role extraction, `500`.
- `GET /materials/{id}` — `200` found, `404` (`KeyNotFoundException`), `500`.
- `GET /materials/bygroup/{groupId}` — `200` with list, `404` (`KeyNotFoundException`), `500`.

#### `FilterConfigRoutesTests` (7 tests)
- `POST /filter-config/filter` — `200` valid data, `400` null body, `500` empty `AffectedRole` triggers `InvalidOperationException`.
- `GET /filter-config/filter/{role}` — `200` known role (seeded via POST), `200` unknown role (returns empty filter).
- `GET /filter-config/filters` — `200` with 2 entries, `200` empty.

#### `CacheManagementRoutesTests` (2 tests)
- `POST /cache/clear` — `200` with success message, `200` when called multiple times.

---

### Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
# Report is written to TestResults/<guid>/coverage.cobertura.xml

# Generate a human-readable HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
open coverage-report/index.html
```
