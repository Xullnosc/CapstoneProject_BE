# FCTMS Backend - .NET Core System

## Overview
This is the backend source code for the FCTMS (FPT Capstone Topic Management System) project. Built with **.NET 8.0**, this system follows an **N-Tier architecture** to ensure scalability, maintainability, and clear separation of concerns.

## System Requirements
- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Database**: [MySQL Server](https://dev.mysql.com/downloads/installer/) (v8.0.44)
- **Migration Tool**: [Flyway Command-line Tool](https://drive.google.com/file/d/1u8jEoIwLmluaFFnbGWtMLWQ9dMAqJ6dZ/view?usp=sharing) (Version **11.13.2**)
- **IDE**: Visual Studio 2022 or VS Code

---

## Getting Started

### 1. Environment Setup
Clone the repository and navigate to the backend root directory:
`..\CapstoneProject_BE`

#### Step 1.1: Install Flyway Binary to Environment Variables
1. Download Flyway version **11.13.2**.
2. Extract the downloaded folder to a permanent location (e.g., `C:\flyway-11.13.2`).
3. **Add to PATH (Windows)**:
   - Press `Win + S` and search for **"Edit the system environment variables"**.
   - **Environment Variables...** will be displayed.
   - Under **User variables**, find and select the **Path** variable, then click **Edit...**.
   - Click **New** and paste the path to the Flyway folder (e.g., `C:\flyway-11.13.2`).
   - Click **OK** on all windows to save.
4. Verify by opening a **new** PowerShell window and running:
   ```powershell
   flyway -v
   ```

#### Step 1.2: Configure Connection Strings & Environment
> [!IMPORTANT]
> The `appsettings.Development.json` file is **git-ignored** for security and must be created manually.

1. Navigate to: `..\CapstoneProject_BE\CapstoneProject_BE\`
2. Create a new file named **`appsettings.Development.json`**.
3. Go to the **Discord** server, find the thread **"Flyway file"**, and **copy the content** provided for this file.
4. Paste the content into your newly created `appsettings.Development.json` and save.
5. Open `..\CapstoneProject_BE\powershell\environment-variables.ps1` and ensure the paths/credentials match your local environment.

---

### 2. Database & Migrations
We use Flyway to manage database schema versions.

1. Open PowerShell as Administrator.
2. Navigate to the `powershell` directory:
   `..\CapstoneProject_BE\powershell`
3. Run the migration script:
   ```powershell
   ./run-flyway.ps1
   ```
   *This script applies all pending migrations to your MySQL database.*

---

### 3. EF Core Scaffolding (MySQL)
To sync your C# Models with the MySQL database schema, run the scaffold command directly.

1. Ensure your database is up-to-date (Step 2).
2. Open PowerShell and navigate to the **BusinessObjects** project directory:
   `..\CapstoneProject_BE\BusinessObjects`
3. Run the following command:
   ```powershell
   dotnet ef dbcontext scaffold "Server=localhost;Database=fctms;UID=root;PWD=YourPassword;AutoEnlist=false" Pomelo.EntityFrameworkCore.MySql --output-dir Models --no-onconfiguring
   ```
   *This command regenerates the entity models in the `Models` folder within `BusinessObjects`.*

---

### 4. Running the Application
From the project root directory `..\CapstoneProject_BE`:
```powershell
dotnet restore
dotnet build
dotnet run --project CapstoneProject_BE
```
- **API URL**: `http://localhost:7046`
- **Swagger UI**: `http://localhost:7046/swagger/index.html`

---

## Project Structure
| Layer | Path | Description |
|-------|------|-------------|
| **API** | `CapstoneProject_BE/` | Web API Layer (Controllers, Config) |
| **Services** | `Services/` | Business Logic Layer |
| **Repositories** | `Repositories/` | Data Access Abstraction |
| **DataAccess** | `DataAccess/` | EF Core DbContext |
| **BusinessObjects** | `BusinessObjects/` | POCO Entities & DTOs |
| **Automation** | `powershell/` | Setup & Migration Scripts |

---

## Testing & Deployment

### Deployment
Dockerfile location: `..\CapstoneProject_BE\Dockerfile`
```powershell
docker build -t fctms-backend .
```

---

## Expected Results
1. **Database**: Tables created in MySQL `fctms` schema.
2. **Models**: C# classes generated in `BusinessObjects/Models`.
3. **Execution**: API running and responding via Swagger UI.

---
*Developed for FCTMS Capstone Project.*
