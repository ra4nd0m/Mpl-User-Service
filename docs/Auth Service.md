# MPL Auth Service
## Overview
MplAuthService is an authentication and user management service built with ASP.NET 
Core. It provides JWT-based authentication, user management, and organization management functionality.
## Software Requirements
* ASP .NET Core 9.0 Runtime
* PostgreSQL DB
## Configuration
The service is configured using appsettings.json files and environment variables.
### Required Configuration Files
1. appsettings.json - Base configuration
2. appsettings.Development.json - Development environment overrides
### Configuration Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mplauthdb;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Key": "your-secret-key-with-at-least-32-characters",
    "Issuer": "mplauthservice",
    "Audience": "mplclient",
    "TokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "AdminInitialization": {
    "Enabled": true,
    "Email": "admin@example.com",
    "Password": "StrongAdminPassword123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
## Running the Service
### Development Environment
```sh
cd src/MplAuthService
dotnet restore
dotnet run
```
### Production Environment
```sh
cd src/MplAuthService
dotnet publish -c Release
dotnet bin/Release/net9.0/MplAuthService.dll
```
## Database Initialization
On startup, the service automatically:

* Applies pending database migrations
* Creates admin user if configured in settings

## Security Features

* Password requirements: minimum 8 characters, requires digits, uppercase, lowercase and special characters
* HTTP-only secure cookies for refresh tokens
* JWT token expiration with refresh mechanism
* Role-based authorization with Admin role

## Authentication Flow

1. User calls ```/login``` with email and password
2. Service returns JWT token and sets HTTP-only refresh token cookie
3. Client includes JWT token in Authorization header for protected requests
4. When JWT expires, client calls ```/refresh``` to get a new token
5. User calls ```/logout``` to invalidate refresh token

## API Endpoints
### Authentication

#### ```POST /login```
* Description: Authenticate user and receive JWT token
* Request Payload:
```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "rememberMe": true
}
```
* Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```
* Notes: Sets an HTTP-only cookie with refresh token

#### ```POST /refresh```
* Description: Refresh an expired JWT token using refresh token
* Request: No body needed, uses refresh token from cookie
* Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### ```POST /logout```
* Description: Invalidate refresh token
* Request: No body needed, uses refresh token from cookie
* Response: Status 200 OK


### User Management (Admin only)

#### ```POST /register```
* Description: Create a new user
* Request Payload:
```json
{
  "email": "newuser@example.com",
  "password": "Password123!",
  "organization": {
    "name": "Example Org",
    "inn": "1234567890",
    "subscriptionType": 1,
    "subscriptionStartDate": "2025-01-01T00:00:00Z",
    "subscriptionEndDate": "2025-12-31T23:59:59Z"
  }
}
```
* Response:
```json
{
  "id": "user-guid-here",
  "email": "newuser@example.com",
  "org": {
    "name": "Example Org",
    "inn": "1234567890",
    "subscriptionType": 1,
    "subscriptionStartDate": "2025-01-01T00:00:00Z",
    "subscriptionEndDate": "2025-12-31T23:59:59Z"
  }
}
```

#### ```GET /users```
* Description: List all users
* Response: Array of user objects
```json
[
  {
    "id": "user-guid-1",
    "email": "user1@example.com",
    "org": {...}
  },
  {
    "id": "user-guid-2",
    "email": "user2@example.com",
    "org": null
  }
]
```

#### ```GET /users/{email}```
* Description: Get user by email
* Response: User object
```json
{
  "id": "user-guid-here",
  "email": "user@example.com",
  "org": {...}
}
```

#### ```DELETE /users/{email}```
* Description: Delete user
* Response: Status 200 OK

### Organization Management (Admin only)

#### ```GET /organizations```

* Description: List all organizations
* Response: Array of organization objects
```json
[
  {
    "name": "Example Org 1",
    "inn": "1234567890",
    "subscriptionType": 1,
    "subscriptionStartDate": "2025-01-01T00:00:00Z",
    "subscriptionEndDate": "2025-12-31T23:59:59Z"
  }
]
```

#### ```GET /organizations/{id}```
* Description: Get organization by ID
* Response: Organization object
```json
{
  "name": "Example Org",
  "inn": "1234567890",
  "subscriptionType": 1,
  "subscriptionStartDate": "2025-01-01T00:00:00Z",
  "subscriptionEndDate": "2025-12-31T23:59:59Z"
}
```




