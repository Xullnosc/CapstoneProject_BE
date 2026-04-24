# FCTMS Backend (.NET 8)

Backend API for the FCTMS (FPT Capstone Topic Management System) project.

## Tech Stack
- .NET 8 (ASP.NET Core Web API)
- MySQL
- Redis (used by several runtime services, including AI features)
- Flyway (database migrations)

## Solution Layout
- `CapstoneProject_BE/`: API host (controllers, startup/configuration)
- `Services/`: business logic
- `Repositories/`: repository layer
- `DataAccess/`: DAO and persistence access
- `BusinessObjects/`: entities, DTOs, shared models
- `FCTMS.Tests/`: test project
- `powershell/`: migration and environment helper scripts

## Prerequisites
- .NET SDK 8.0+
- MySQL 8+
- Redis 7+
- Flyway CLI (if you run migrations locally)

## Local Setup

### 1. Create local development config
Create `CapstoneProject_BE/appsettings.Development.json`.

Use this template and replace values with your local credentials/secrets:

```json
{
  "ConnectionStrings": {
    "capstoneDb": "Server=localhost;Database=fctms;User=YOUR_USER;Password=YOUR_PASSWORD;AutoEnlist=false"
  },
  "Redis": {
    "Connection": "localhost:6379"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong",
    "Issuer": "FCTMSBackend",
    "Audience": "FCTMSFrontend"
  },
  "AllowedOrigins": "http://localhost:5173"
}
```

Notes:
- `Jwt:Key` is required at startup.
- `ConnectionStrings:capstoneDb` is required by EF Core `DbContext` registration.

### 2. Prepare database
Create database:

```sql
CREATE DATABASE fctms;
```

Run Flyway migrations from the backend root:

```powershell
cd powershell
./run-flyway.ps1
```

If needed, update local paths/variables in `powershell/environment-variables.ps1`.

### 3. Run backend API
From backend root (`Capstone_BE/`):

```powershell
dotnet restore capstone_be.sln
dotnet build capstone_be.sln
dotnet run --project CapstoneProject_BE/capstone_be.csproj --launch-profile http
```

Default local URL:
- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

## Tests
Run all tests:

```powershell
dotnet test FCTMS.Tests/FCTMS.Tests.csproj
```

## Docker (Backend + Redis)
From backend root:

```powershell
docker compose up --build
```

Compose file:
- `docker-compose.yml`

Dockerfile:
- `Dockerfile`

## AI Module Notes
AI pipeline documentation is available in:
- `AI_WORKFLOW.md`

## Troubleshooting

### `dotnet run` exits with code 1
Check these first:
1. `CapstoneProject_BE/appsettings.Development.json` exists.
2. `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` are present.
3. `ConnectionStrings:capstoneDb` is valid.
4. Redis is reachable at `Redis:Connection`.

Quick Redis start (Docker):

```powershell
docker run --name fctms-redis -p 6379:6379 -d redis:7
```

### Flyway command not found
Install Flyway CLI and ensure it is in `PATH`, then reopen terminal.

---
Developed for the FCTMS Capstone project.
