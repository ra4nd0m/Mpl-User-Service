# MplAuthService

A .NET 10 authentication and user-management microservice that handles JWT-based authentication, refresh tokens, user lifecycle, and organization management. Part of the **Mpl-User-Service** mono-repo.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [API Reference](#api-reference)
  - [Authentication](#authentication)
  - [User Management](#user-management)
  - [Organizations](#organizations)
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

MplAuthService provides:

- **JWT authentication** with short-lived access tokens and long-lived HTTP-only refresh tokens
- **Role-based authorization** (`Admin` / regular user) enforced via an `AdminOnly` policy
- **Subscription enforcement** — non-admin users with an expired organization or individual subscription are blocked at login and token-refresh time
- **User management** — create, update, list, and delete users, with optional organization or individual subscription linking
- **Organization management** — full CRUD for organizations, including listing their members
- **Data Protection** — keys persisted in PostgreSQL, protected with an X.509 certificate
- **External user API** integration via `IHttpClientFactory`

---

## Tech Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 (Minimal APIs) |
| ORM | Entity Framework Core 10 + Npgsql |
| Identity | ASP.NET Core Identity |
| Authentication | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Data Protection | `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` |
| Database | PostgreSQL |
| Target Runtime | .NET 10 |

---

## Project Structure

```
auth/
├── src/
│   └── MplAuthService/
│       ├── Program.cs                  # DI setup, middleware pipeline, route registration
│       ├── appsettings.json            # Base configuration
│       ├── appsettings.Development.json
│       ├── Data/
│       │   └── AuthContext.cs          # EF Core DbContext
│       ├── Interfaces/                 # Service contracts
│       │   ├── IJwtService.cs
│       │   ├── IOrgService.cs
│       │   ├── IRefreshTokenService.cs
│       │   └── IUserService.cs
│       ├── Migrations/                 # EF Core migration history
│       ├── Models/
│       │   ├── User.cs
│       │   ├── Organization.cs
│       │   ├── RefreshToken.cs
│       │   ├── IndividualSubscription.cs
│       │   ├── Dtos/                   # Request / response DTOs
│       │   └── Enums/
│       │       └── SubscriptionType.cs
│       ├── Routes/
│       │   ├── AuthRoutes.cs           # /login  /refresh  /logout
│       │   ├── UserManagementRoutes.cs # /register  /users  /users/{email}
│       │   └── OrgRoutes.cs           # /organizations  /organizations/{inn|id}
│       ├── Services/
│       │   ├── JwtService.cs
│       │   ├── RefreshTokenService.cs
│       │   ├── UserService.cs
│       │   └── OrgService.cs
│       └── Utils/
│           ├── DatabaseInitializer.cs  # Seeds admin account on startup
│           └── EmailObfuscator.cs      # Safe email logging
└── tests/
    └── MplAuthService.Tests/
        ├── Services/                   # Unit tests for service classes
        └── Routes/                     # Integration tests for HTTP endpoints
```

---

## Configuration

All settings are in `appsettings.json` and can be overridden by environment variables.

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | _(required)_ |
| `Jwt:Key` | HMAC-SHA256 signing key (min 32 chars) | _(required)_ |
| `Jwt:Issuer` | JWT `iss` claim | `MplAuth` |
| `Jwt:Audience` | JWT `aud` claim | `MplService` |
| `Jwt:TokenExpiryMinutes` | Access token lifetime | `30` |
| `Jwt:RefreshTokenExpiryDays` | Refresh token lifetime | `1` |
| `Cert:Path` | Path to `.pfx` certificate for Data Protection | _(required)_ |
| `Cert:Password` | Password for the `.pfx` certificate | _(required)_ |
| `AdminInitialization:Enabled` | Seed an admin account on startup | `false` |
| `AdminInitialization:Email` | Seeded admin email | `admin@example.com` |
| `AdminInitialization:Password` | Seeded admin password | `AdminPassword123!` |
| `Cors:AllowedOrigins` | Array of allowed CORS origins | `[]` |
| `ExternalUserApi:BaseUrl` | Base URL of the external user API | `http://localhost:5201` |
| `Kestrel:Endpoints:Http:Url` | Bind address | `http://*:5203` |

### Password policy

Passwords must be at least 8 characters and include an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.

---

## API Reference

All endpoints that modify or read sensitive data require an `Authorization: Bearer <token>` header (enforced via the `AdminOnly` policy) unless noted otherwise.

### Authentication

#### `POST /login`

Authenticates a user. Returns a short-lived JWT and sets an HTTP-only `refreshToken` cookie.

**Request body**
```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "rememberMe": false
}
```

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | `{ "token": "<jwt>" }` |
| `400 Bad Request` | Invalid email or password |
| `403 Forbidden` | Subscription expired |

---

#### `POST /refresh`

Exchanges a valid `refreshToken` cookie for a new JWT. Does **not** rotate the refresh token.

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | `{ "token": "<new_jwt>" }` |
| `401 Unauthorized` | Cookie absent or token invalid/expired |
| `403 Forbidden` | Subscription expired |

---

#### `POST /logout`

Invalidates the current refresh token and clears the cookie.

**Responses**

| Status | Meaning |
|---|---|
| `200 OK` | Always (idempotent) |

---

### User Management

All endpoints require `AdminOnly` authorization.

#### `POST /register`

Creates a new user, optionally linking them to an organization or an individual subscription.

**Request body** (`CreateUserDto`)
```json
{
  "email": "newuser@example.com",
  "password": "Password123!",
  "organization": { ... },   // optional
  "sub": { ... },            // optional IndividualSubscription
  "canExportData": false
}
```

**Responses**: `200 OK` with `UserResponseDto` | `400 Bad Request`

---

#### `PUT /users/{email}`

Updates an existing user's password, organization, or individual subscription.

**Responses**: `200 OK` with `UserResponseDto` | `400 Bad Request` | `404 Not Found`

---

#### `GET /users`

Returns all users with their organization or subscription information.

**Responses**: `200 OK` with array of `UserResponseDto` | `400 Bad Request`

---

#### `GET /users/{email}`

Returns a single user by email.

**Responses**: `200 OK` with `UserResponseDto` | `404 Not Found`

---

#### `DELETE /users/{email}`

Deletes a user and all associated refresh tokens.

**Responses**: `200 OK` | `404 Not Found` | `400 Bad Request`

---

### Organizations

All endpoints require `AdminOnly` authorization.

#### `GET /organizations`

Returns all organizations.

**Responses**: `200 OK` with array of `OrganizationDto` | `400 Bad Request`

---

#### `GET /organizations/{inn}`

Finds an organization by its INN (tax identification number).

**Responses**: `200 OK` with `OrganizationDto` | `404 Not Found` | `400 Bad Request`

---

#### `GET /organizations/{orgId}/users`

Returns all users belonging to an organization.

**Responses**: `200 OK` with array of users | `400 Bad Request`

---

#### `PUT /organizations/{id}`

Updates an organization's details.

**Responses**: `200 OK` | `404 Not Found` | `400 Bad Request`

---

#### `POST /organizations`

Creates a new organization.

**Request body** (`OrganizationDto`)
```json
{
  "name": "ACME Corp",
  "inn": "1234567890",
  "subscriptionType": 1,
  "subscriptionStartDate": "2025-01-01T00:00:00Z",
  "subscriptionEndDate": "2026-01-01T00:00:00Z"
}
```

**Responses**: `200 OK` with created `OrganizationDto` (includes assigned `id`) | `400 Bad Request`

---

#### `DELETE /organizations/{id}`

Deletes an organization.

**Responses**: `200 OK` | `404 Not Found` | `400 Bad Request`

---

## Running the Service

### Prerequisites

- .NET 10 SDK
- PostgreSQL instance
- A valid `.pfx` certificate for Data Protection

### Steps

```bash
# 1. Configure connection string, JWT key, and certificate path
# Edit appsettings.Development.json or set environment variables

# 2. Apply migrations
cd src/MplAuthService
dotnet ef database update

# 3. Run
dotnet run
# Service listens on http://*:5203 by default
```

---

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/MplAuthService

# Apply to database
dotnet ef database update --project src/MplAuthService

# Rollback to a specific migration
dotnet ef database update <MigrationName> --project src/MplAuthService
```

---

## Tests

The test project lives in `tests/MplAuthService.Tests/` and targets .NET 10.

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
tests/MplAuthService.Tests/
├── Services/                        # Unit tests — no HTTP layer
│   ├── JwtServiceTests.cs           # 9 tests
│   ├── RefreshTokenServiceTests.cs  # 8 tests
│   ├── OrgServiceTests.cs           # 12 tests
│   └── UserServiceTests.cs          # 21 tests
└── Routes/                          # Integration tests — real HTTP via TestHost
    ├── Helpers/
    │   ├── TestAuthHandler.cs       # Bypasses JWT auth; always returns Admin identity
    │   └── RouteTestHost.cs         # Shared host-builder utility
    ├── AuthRoutesTests.cs           # 12 tests
    ├── UserManagementRoutesTests.cs # 13 tests
    └── OrgRoutesTests.cs            # 13 tests
```

**Total: 88 tests**

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=normal"

# Run a specific test class
dotnet test --filter "ClassName=JwtServiceTests"

# Run a specific test method
dotnet test --filter "FullyQualifiedName~Login_Returns200_WithValidCredentials"
```

### Service Tests

Unit tests exercise each service class in isolation. EF Core's InMemory provider is used for the database; `UserManager<User>` and other ASP.NET Core Identity types are mocked with Moq.

**Key pattern**

```csharp
var options = new DbContextOptionsBuilder<AuthContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())   // unique DB per test
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
    .Options;

using var context = new AuthContext(options);
var service = new OrgService(context);
```

> `TransactionIgnoredWarning` is suppressed because `UserService` calls `BeginTransactionAsync`, which EF Core InMemory treats as a no-op and warns about by default.

#### `JwtServiceTests` (9 tests)
- `GenerateJwtToken` — verifies token validity, expected claims (email, role, org, subscription), signing key, and throws when the JWT key is missing.
- `GenerateInternalToken` — verifies the token contains the `Internal` role claim.

#### `RefreshTokenServiceTests` (8 tests)
- `GenerateRefreshToken` — token is persisted, contains the correct user ID, and expires after the configured number of days.
- `ValidateRefreshToken` — returns token when valid, `null` when not found, `null` when expired, `null` after recall.
- `RecallRefreshToken` — marks the token as expired; is a no-op when the token does not exist.

#### `OrgServiceTests` (12 tests)
- `GetOrganization` — found by INN and not found (`null`).
- `GetOrganizations` — non-empty and empty list.
- `CreateOrganization` — persists a new org; throws on duplicate INN.
- `UpdateOrganization` — updates fields; returns `null` for missing ID.
- `DeleteOrganization` — returns `true` on success and `false` when not found.
- `GetUsersByOrganization` — returns member list and empty list.

#### `UserServiceTests` (21 tests)
- `CreateUser` — covers: user already exists (throws), no org/sub, with both, new organization created, reuses existing org, individual subscription, role auto-creation, and identity failure propagation.
- `CreateAdmin` — success, duplicate detection, and admin role auto-creation.
- `GetUserByEmail` — empty email throws `ArgumentException`; not found throws `Exception`; success returns the user.
- `GetUsers` — full list and empty list.
- `DeleteUser` — removes associated refresh tokens before deletion; propagates identity failure.

---

### Route Tests

Integration tests spin up a real `WebApplication` using `UseTestServer()`. Each test class builds its own isolated host — no shared state between classes.

**Key pattern**

```csharp
var builder = WebApplication.CreateSlimBuilder();
builder.WebHost.UseTestServer();

// InMemory DB
builder.Services.AddDbContext<AuthContext>(options =>
    options.UseInMemoryDatabase(Guid.NewGuid().ToString())
           .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

// Replace real services with mocks
builder.Services.AddScoped(_ => _userServiceMock.Object);

// Replace JWT auth with a test handler that always authenticates as Admin
builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(o =>
    o.AddPolicy("AdminOnly", p => p.RequireAuthenticatedUser()));

builder.Logging.ClearProviders();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapUserManagementRoutes();
await app.StartAsync();

_client = app.GetTestClient();
```

**`TestAuthHandler`** (`Routes/Helpers/TestAuthHandler.cs`) is a custom `AuthenticationHandler` that always returns an authenticated `ClaimsPrincipal` with an `Admin` role claim, allowing all `AdminOnly`-protected routes to be tested without a real JWT.

#### `AuthRoutesTests` (12 tests)
- `/login` — `200` valid credentials (verifies `Set-Cookie` header), `400` wrong password, `400` user not found, `403` expired org subscription, `403` expired individual subscription, `200` admin bypasses subscription check.
- `/refresh` — `401` no cookie, `401` invalid token, `200` valid token, `403` expired subscription.
- `/logout` — `200` with cookie (calls `RecallRefreshToken`), `200` without cookie (no-op).

#### `UserManagementRoutesTests` (13 tests)
- `/register` — `200` with org DTO, `400` on service exception.
- `/users/{email}` (PUT) — `200` with org DTO, `200` with subscription DTO, `400` on service exception.
- `/users` (GET) — `200` with 2 users, `200` empty list.
- `/users/{email}` (GET) — `200` found, `404` when service returns `null`.
- `/users/{email}` (DELETE) — `200` success, `404` not found, `400` on service exception.

#### `OrgRoutesTests` (13 tests)
- `/organizations` — `200` with 2 orgs, `200` empty.
- `/organizations/{inn}` — `200` found, `404` not found.
- `/organizations/{orgId}/users` — `200` with list, `400` on service exception.
- `/organizations/{id}` (PUT) — `200`, `404`, `400`.
- `/organizations` (POST) — `200` with assigned ID, `400`.
- `/organizations/{id}` (DELETE) — `200`, `404`, `400`.

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
