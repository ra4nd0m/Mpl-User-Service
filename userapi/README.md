# MplUserService

A .NET 10 user-data and file-management microservice that stores user preferences, favourite IDs, and report files, and proxies requests to the DB and spreadsheet generator APIs. Part of the **Mpl-User-Service** mono-repo.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [API Reference](#api-reference)
  - [User Data](#user-data)
  - [Report Files](#report-files)
  - [Data Proxy](#data-proxy)
  - [Generator Proxy](#generator-proxy)
  - [Internal](#internal)
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

MplUserService provides:

- **User preferences** — per-user favourite item IDs and an arbitrary settings JSON blob, persisted in PostgreSQL
- **Report file management** — upload, list, download, and delete PDF report files stored on disk, with per-file subscription-level access control
- **Data proxy** — forwards `GET`/`POST` requests to the DB API service, passing through the caller's authorization header
- **Generator proxy** — forwards `POST` requests to the spreadsheet generator service, gated by a custom `CanExportData` claim
- **Internal user deletion** — an `internal`-role-only endpoint for deleting a user record on account removal
- **Subscription-aware authorization** — custom `SubscriptionHandler` and `RoleHandler` enforce minimum subscription tiers on endpoints

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Minimal APIs) |
| ORM | Entity Framework Core 10 + Npgsql |
| Authentication | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| File Storage | Local disk via `DiskObjectStoreService` |
| Database | PostgreSQL |
| Target Runtime | .NET 10 |

---

## Project Structure

```
userapi/
├── src/
│   └── MplUserService/
│       ├── Program.cs                    # DI setup, middleware pipeline, route registration
│       ├── appsettings.json              # Base configuration
│       ├── appsettings.Development.json
│       ├── Auth/
│       │   ├── CanExportData.cs          # CanExportData policy requirement + handler
│       │   ├── RoleHandler.cs            # Generic role-based requirement + handler
│       │   └── SubscriptionHandler.cs    # Subscription-tier requirement + handler
│       ├── Config/
│       │   └── StorageQuotaOptions.cs    # Typed options for storage quota
│       ├── Data/
│       │   └── UserContext.cs            # EF Core DbContext (Users, ReportFiles)
│       ├── Extensions/
│       │   └── MappingExtensions.cs      # Model → DTO mapping helpers
│       ├── Interfaces/
│       │   ├── IObjectStore.cs           # File-store abstraction
│       │   ├── IReportFileService.cs     # Report file business logic contract
│       │   └── IUserService.cs           # User business logic contract
│       ├── Migrations/                   # EF Core migration history
│       ├── Models/
│       │   ├── User.cs
│       │   ├── ReportFile.cs
│       │   ├── Dtos/                     # Response DTOs
│       │   │   ├── ReportFilesListDto.cs
│       │   │   └── StorageUsageDto.cs
│       │   └── Enums/
│       │       └── SubscriptionType.cs
│       ├── Routes/
│       │   ├── UserDataRoutes.cs         # /favorites  /settings
│       │   ├── ReportFileRoutes.cs       # /reports  /reports/{id}  /reports/upload  /reports/storage-usage
│       │   ├── DbApiRoutes.cs            # /data/{**}  /data/filter-config/{**}
│       │   ├── GeneratorRoutes.cs        # /generator/spreadsheet/{**}
│       │   └── InternalRoutes.cs         # /user/{userId}
│       └── Services/
│           ├── UserService.cs
│           ├── ReportFileService.cs
│           ├── DiskObjectStoreService.cs
│           └── DbApiService.cs
└── tests/
    └── MplUserService.Tests/
        ├── Services/                     # Unit tests for service classes
        └── Routes/                       # Integration tests for HTTP endpoints
```

---

## Configuration

All settings live in `appsettings.json` and can be overridden by environment variables.

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | _(required)_ |
| `Jwt:Issuer` | Expected JWT `iss` claim | `MplAuth` |
| `Jwt:Audience` | Expected JWT `aud` claim | `MplService` |
| `Jwt:Key` | HMAC-SHA256 key used to validate incoming tokens | _(required)_ |
| `Storage:RootPath` | Root directory for report file storage | _(required)_ |
| `StorageQuota:MaxBytes` | Maximum total storage for report files | `5368709120` (5 GB) |
| `DBApi:BaseUrl` | Base URL of the DB API service | `http://localhost:5201` |
| `SpreadsheetApi:BaseUrl` | Base URL of the spreadsheet generator service | _(required)_ |
| `Cors:AllowedOrigins` | Array of allowed CORS origins | `[]` |
| `Hosting:Urls` | Override Kestrel bind address | `http://*:5204` |

---

## API Reference

Endpoints require an `Authorization: Bearer <token>` header unless noted otherwise. The token is issued by **MplAuthService** and carries subscription, role, and `CanExportData` claims.

### User Data

#### `GET /favorites`

Returns the authenticated user's list of favourite item IDs.

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | `[1, 2, 3]` |
| `400 Bad Request` | Unexpected error |

---

#### `PUT /favorites/{itemId}`

Adds an item to the user's favourites (no-op if already present).

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | Updated favourite ID list |
| `400 Bad Request` | `itemId` ≤ 0, or unexpected error |

---

#### `DELETE /favorites/{itemId}`

Removes an item from the user's favourites (no-op if absent).

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | Updated favourite ID list |
| `400 Bad Request` | `itemId` ≤ 0, or unexpected error |

---

#### `POST /favorites`

Replaces the user's entire favourite list.

**Request body**: JSON array of integers, e.g. `[10, 20, 30]`

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | New favourite ID list |
| `400 Bad Request` | Empty array, invalid JSON, or unexpected error |

---

#### `GET /settings`

Returns the user's settings blob.

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | Raw settings JSON string |
| `404 Not Found` | No settings saved yet |
| `400 Bad Request` | Unexpected error |

---

#### `PUT /settings`

Saves (replaces) the user's settings blob.

**Request body**: Any valid JSON object.

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | Settings saved |
| `400 Bad Request` | Invalid JSON or unexpected error |

---

### Report Files

#### `GET /reports`

Lists all available report files. Rate-limited by `DownloadPolicy`.

**Responses**: `200 OK` with array of `ReportFilesListDto` | `400 Bad Request`

---

#### `GET /reports/{id:guid}`

Downloads a report file as `application/pdf`. Access is controlled by each file's `RequiredSubscription`.

**Responses**: `200 OK` (stream) | `404 Not Found` (file missing or insufficient subscription) | `400 Bad Request`

---

#### `POST /reports/upload`

Uploads a new report file. Requires `Admin` role. Accepts `multipart/form-data` with fields:

| Field | Type | Description |
|---|---|---|
| `file` | file | The PDF file to upload |
| `requiredSubscription` | string | `Free` \| `Basic` \| `Premium` |
| `group` | string | Logical grouping label for the file |

**Responses**: `200 OK` with `{ "id": "<guid>" }` | `400 Bad Request`

---

#### `DELETE /reports/{id:guid}`

Deletes a report file. Requires `Admin` role.

**Responses**: `200 OK` | `400 Bad Request`

---

#### `GET /reports/storage-usage`

Returns current disk usage vs. configured quota. Requires `Admin` role.

**Responses**: `200 OK` with `StorageUsageDto` | `400 Bad Request`

---

### Data Proxy

Proxies requests to the configured DB API service, forwarding the caller's `Authorization` header.

| Method | Path | Auth policy |
|---|---|---|
| `GET` | `/data/filter-config/{**catchAll}` | `RequireAdmin` |
| `POST` | `/data/filter-config/{**catchAll}` | `RequireAdmin` |
| `GET` | `/data/{**catchAll}` | JWT bearer |
| `POST` | `/data/{**catchAll}` | JWT bearer |

---

### Generator Proxy

Proxies `POST` requests to the spreadsheet generator service. Requires the `CanExportData` claim to be `true`.

| Method | Path |
|---|---|
| `POST` | `/generator/spreadsheet/{**catchAll}` |

---

### Internal

#### `DELETE /user/{userId}`

Deletes a user record. Requires the `internal` role (service-to-service only).

**Responses**: `200 OK` | `404 Not Found` | `400 Bad Request`

---

## Running the Service

### Prerequisites

- .NET 10 SDK
- PostgreSQL instance
- A writable directory for report file storage

### Steps

```bash
# 1. Configure the connection string, JWT key, storage path, and downstream API URLs
# Edit appsettings.Development.json or set environment variables

# 2. Apply migrations
cd src/MplUserService
dotnet ef database update

# 3. Run
dotnet run
# Service listens on http://*:5204 by default
```

---

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/MplUserService

# Apply to database
dotnet ef database update --project src/MplUserService

# Rollback to a specific migration
dotnet ef database update <MigrationName> --project src/MplUserService
```

---

## Tests

The test project lives in `tests/MplUserService.Tests/` and targets .NET 10.

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
tests/MplUserService.Tests/
├── Services/                          # Unit tests — no HTTP layer
│   ├── UserServiceTests.cs            # 7 tests
│   ├── ReportFileServiceTests.cs      # 12 tests
│   └── DiskObjectStoreServiceTests.cs # 9 tests
└── Routes/                            # Integration tests — real HTTP via TestHost
    ├── Helpers/
    │   └── TestAuthHandler.cs         # Bypasses JWT auth; always returns Admin identity
    ├── UserDataRoutesTests.cs         # 21 tests
    ├── ReportFileRoutesTests.cs       # 16 tests
    └── InternalRoutesTests.cs         # 3 tests
```

**Total: 68 tests**

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=normal"

# Run a specific test class
dotnet test --filter "ClassName=UserServiceTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~GetFavorites_Returns200_WithFavoriteIds"

# Run only route tests
dotnet test --filter "FullyQualifiedName~MplUserService.Tests.Routes"

# Run only service tests
dotnet test --filter "FullyQualifiedName~MplUserService.Tests.Services"
```

### Service Tests

Unit tests exercise each service class in isolation. EF Core's InMemory provider replaces PostgreSQL; `IObjectStore`, `IAuthorizationService`, and other infrastructure types are mocked with Moq.

**Key pattern**

```csharp
var options = new DbContextOptionsBuilder<UserContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())   // unique DB per test
    .Options;

using var context = new UserContext(options);
var service = new UserService(context);
```

#### `UserServiceTests` (7 tests)
- `GetOrCreateUserAsync` — creates a new user when absent, returns the existing user when found, throws `UnauthorizedException` when the `NameIdentifier` claim is missing.
- `DeleteUserAsync` — deletes the user and returns `true`; throws `InvalidOperationException` when not found; throws `ArgumentNullException` for a `null` or empty user ID.

#### `ReportFileServiceTests` (12 tests)
- `UploadAsync` — throws on empty file, throws when storage quota is exceeded, creates file and returns ID on success.
- `ListAsync` — returns all files, returns empty list when none exist.
- `DownloadAsync` — throws `UnauthorizedException` when file does not exist, throws when authorization check fails, returns stream and file name when authorized.
- `DeleteAsync` — removes the file from the store and the database; is a no-op when file does not exist.
- `GetReportStorageUsageAsync` — returns correct used/max bytes, returns zero usage when no files are stored.

#### `DiskObjectStoreServiceTests` (9 tests)
- `PutAsync` — creates a file with correct content, overwrites an existing file, handles an empty stream, handles large content.
- `GetAsync` — returns a readable stream with content type when the file exists, throws `FileNotFoundException` when absent, returns a readable stream.
- `DeleteAsync` — deletes the file when it exists, does not throw when the file is absent.

---

### Route Tests

Integration tests spin up a real `WebApplication` using `UseTestServer()`. Each test class builds its own isolated host — no shared state between classes.

**Key pattern**

```csharp
var builder = WebApplication.CreateSlimBuilder();
builder.WebHost.UseTestServer();

// InMemory DB (where needed)
var dbOptions = new DbContextOptionsBuilder<UserContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
    .Options;
builder.Services.AddSingleton(new UserContext(dbOptions));

// Replace real services with mocks
builder.Services.AddSingleton(_userServiceMock.Object);

// Replace JWT auth with a test handler that always authenticates as Admin
builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Logging.ClearProviders();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapUserDataRoutes();
await app.StartAsync();

_client = app.GetTestClient();
```

**`TestAuthHandler`** (`Routes/Helpers/TestAuthHandler.cs`) is a custom `AuthenticationHandler` that always returns an authenticated `ClaimsPrincipal` with an `Admin` role and `CanExportData=true`, allowing all protected routes to be exercised without a real JWT.

#### `UserDataRoutesTests` (21 tests)
- `GET /favorites` — `200` with list, `200` empty list, `400` on service exception.
- `PUT /favorites/{itemId}` — `200` item added, `200` no-op when already present (no duplicate), `400` for `itemId` = 0, `400` on service exception.
- `DELETE /favorites/{itemId}` — `200` item removed, `200` no-op when absent, `400` for `itemId` = 0, `400` on service exception.
- `POST /favorites` — `200` list replaced, `400` empty array, `400` invalid JSON, `400` on service exception.
- `GET /settings` — `200` when settings exist, `404` when null, `400` on service exception.
- `PUT /settings` — `200` valid JSON saved, `400` invalid JSON, `400` on service exception.

#### `ReportFileRoutesTests` (16 tests)
- `GET /reports` — `200` with 2 files, `200` empty, `400` on service exception.
- `GET /reports/{id}` — `200` with PDF stream, `404` on `UnauthorizedAccessException`, `400` on other exceptions.
- `POST /reports/upload` — `200` with generated GUID, `400` missing file part, `400` invalid subscription value, `400` non-form content type, `400` on service exception.
- `DELETE /reports/{id}` — `200` success (verifies service call), `400` on service exception.
- `GET /reports/storage-usage` — `200` with usage DTO, `400` on `InvalidOperationException` (quota not configured), `400` on other exceptions.

#### `InternalRoutesTests` (3 tests)
- `DELETE /user/{userId}` — `200` user deleted, `404` user not found, `400` on service exception.

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
