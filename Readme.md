# MPL User Service

A comprehensive microservices platform for managing material market price data, user authentication, and analytics. The system consists of multiple independent services working together to provide subscription-based access to material price lists, time-series market data, user management, report generation, and a modern web interface.

## Architecture Overview

The system is composed of the following services:

### Core Services

- **MplAuthService** - JWT-based authentication with refresh tokens, user lifecycle management, organization management, and subscription enforcement
- **MplUserService** - User preferences, favorite items, report file storage with disk-based object store, and proxy endpoints to DB/generator APIs
- **MplDbApi** - Material market data API with time-series values, role-based filtering, reference data, and in-memory caching
- **MplDataReceiver** - Service for receiving and processing incoming MPL data updates

### Supporting Components

- **UI** - Modern SvelteKit-based web interface
- **SpreadsheetGen** - TypeScript/Node.js service for generating Excel spreadsheets
- **Parsers** - Python-based parsers for analytics and LME data

## Tech Stack

### Backend Services
- **Runtime**: .NET 10.0 (ASP.NET Core Minimal APIs)
- **Databases**: PostgreSQL, SQLite
- **Authentication**: JWT Bearer with refresh tokens
- **ORM**: Entity Framework Core 10 + Npgsql
- **Testing**: xUnit with in-memory databases

### Frontend
- **Framework**: SvelteKit 2.x
- **Language**: TypeScript
- **Build Tool**: Vite
- **Charts**: Chart.js
- **Internationalization**: Paraglide-JS

### Generators & Parsers
- **Node.js/TypeScript**: Spreadsheet generation (XLSX)
- **Python**: Data parsing and analytics

## Prerequisites

- .NET 10.0 SDK and Runtime
- Node.js 18+ and npm
- Python 3.8+
- PostgreSQL 14+
- SQLite 3
- X.509 certificate (for production data protection)

## Project Structure

```
.
├── auth/                    # Authentication service
│   ├── src/MplAuthService/  # Service implementation
│   ├── tests/               # xUnit test suite
│   └── README.md            # Detailed API documentation
├── userapi/                 # User API service
│   ├── src/MplUserService/  # Service implementation
│   ├── tests/               # xUnit test suite
│   └── README.md            # Detailed API documentation
├── db/                      # Database API service
│   ├── src/MplDbApi/        # Service implementation
│   ├── tests/               # xUnit test suite
│   └── README.md            # Detailed API documentation
├── parsers/
│   ├── data-receiver/       # Data receiver service
│   └── externally-scheduled/ # Batch parsers
├── generators/
│   └── spreadsheetGen/      # Excel generation service
├── ui/                      # SvelteKit frontend
│   └── src/                 # Svelte components and routes
├── deploy/                  # Deployment automation
│   ├── build-scripts/       # Build scripts for each service
│   ├── systemd-files/       # Systemd service files
│   └── update-scripts/      # Update scripts for deployments
├── docs/                    # Additional documentation
└── releases/                # Built artifacts
```

## Configuration

Each service requires its own configuration. See individual service directories for detailed configuration requirements:

### Common Configuration Pattern

All .NET services use `appsettings.json` and `appsettings.{Environment}.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=dbname;Username=user;Password=pass"
  },
  "Jwt": {
    "Key": "your-secret-key-with-at-least-32-characters",
    "Issuer": "issuer",
    "Audience": "audience"
  }
}
```

## Development Setup

### 1. Clone the Repository

```bash
git clone https://github.com/ra4nd0m/Mpl-User-Service.git
cd Mpl-User-Service
```

### 2. Setup Databases

Create required PostgreSQL databases:
```bash
createdb mplauthdb
createdb mpluserdb
createdb mpldbapi
```

### 3. Configure Services

Update `appsettings.Development.json` in each service directory with your local database connections and JWT settings.

### 4. Run Services

#### Auth Service
```bash
cd auth/src/MplAuthService
dotnet restore
dotnet run
```

#### User API Service
```bash
cd userapi/src/MplUserService
dotnet restore
dotnet run
```

#### Database API Service
```bash
cd db/src/MplDbApi
dotnet restore
dotnet run
```

#### Data Receiver Service
```bash
cd parsers/data-receiver/MplDataReceiver
dotnet restore
dotnet run
```

#### Spreadsheet Generator
```bash
cd generators/spreadsheetGen
npm install
npm run dev
```

#### UI
```bash
cd ui
npm install
npm run dev
```

## Production Deployment

The project includes automated build and deployment scripts using systemd.

### Building Services

```bash
# Build all services
cd deploy/build-scripts
./build-auth.sh
./build-userapi.sh
./build-dbapi.sh
./build-data-receiver.sh
./build-spreadsheet-gen.sh
./build-ui.sh
```

### Installing Services

1. Copy systemd service files and ensure they're filled:
```bash
sudo cp deploy/systemd-files/*.service /etc/systemd/system/
```

2. Enable and start services:
```bash
sudo systemctl enable mplauth mpluser mpldbapi mpl-data-receiver mpl-spreadsheet-gen
sudo systemctl start mplauth mpluser mpldbapi mpl-data-receiver mpl-spreadsheet-gen
```

### Updating Services

```bash
cd deploy/update-scripts
./update-auth.sh
./update-userapi.sh
./update-dbapi.sh
./update-data-receiver.sh
./update-ui.sh
```

## Testing

Each .NET service includes a comprehensive test suite using xUnit:

### Running Tests

```bash
# Run all tests for a specific service
cd auth && dotnet test
cd userapi && dotnet test
cd db && dotnet test

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

Each test project includes:
- **Service Tests** - Unit tests for business logic services
- **Route Tests** - Integration tests for API endpoints
- **Helper Classes** - Test utilities and mock implementations

## API Documentation

Detailed API documentation is available for each service:

- **Auth Service**: [auth/README.md](auth/README.md) - Authentication, user management, and organizations
- **User API**: [userapi/README.md](userapi/README.md) - User data, file storage, and API proxies
- **Database API**: [db/README.md](db/README.md) - Material data, metrics, and filter configuration

Each service README includes:
- Complete API endpoint reference with request/response examples
- Configuration requirements
- Database migration instructions
- Testing guidelines

## Security Features

- **JWT Authentication** - Short-lived access tokens with long-lived HTTP-only refresh tokens
- **Role-Based Authorization** - Admin and user roles with custom policy handlers
- **Subscription Enforcement** - Organization and individual subscription validation at authentication time
- **ASP.NET Core Identity** - Password hashing with configurable complexity requirements
- **Data Protection** - Keys persisted in PostgreSQL and protected with X.509 certificates
- **Rate Limiting** - Configured on sensitive endpoints to prevent abuse
- **CORS Configuration** - Controlled cross-origin access for web clients
- **Claim-Based Authorization** - Custom claims like `CanExportData` for fine-grained permissions
- **File Access Control** - Subscription-level gating on report file downloads
