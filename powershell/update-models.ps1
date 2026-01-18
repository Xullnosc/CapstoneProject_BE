# to update models by deleting them first and then rebuild the models folder from scratch
# run in the project folder directory
$envFile = Join-Path $PSScriptRoot "environment-variables.ps1"
if (Test-Path $envFile) {
    . $envFile
    Write-Host "Sourced environment variables from: $envFile"
}
else {
    Write-Verbose "No environment-variables.ps1 found at $envFile; continuing without it."
}

# Dot-source functions helper if present
$funcFile = Join-Path $PSScriptRoot "functions.ps1"
if (Test-Path $funcFile) {
    . $funcFile
}
else {
    throw "Required helper script not found: $funcFile"
}

# Try to get connection string. environment-variables.ps1 may set $devConfigPath and $defaultConfigPath
try {
    if ($devConfigPath -and (Test-Path $devConfigPath)) {
        $conn = Get-ConnectionString -SearchPaths @($devConfigPath) -Key 'capstoneDb'
    }
    elseif ($defaultConfigPath -and (Test-Path $defaultConfigPath)) {
        $conn = Get-ConnectionString -SearchPaths @($defaultConfigPath) -Key 'capstoneDb'
    }
    else {
        $conn = Get-ConnectionString
    }
}
catch {
    throw "Unable to determine connection string: $_"
}

if (Test-Path ($modelsFolder)) {
    Remove-Item -Recurse -Force ($modelsFolder)
    Write-Host "|-------------------------------------------|"
    Write-Host "|---------- Old models deleted -------------|"
    Write-Host "|-------------------------------------------|"
}


Write-Host "Connection: $($conn.Substring(0,[Math]::Min(80,$conn.Length)))"

# Ensure the BusinessObjects project path and models folder are absolute
$projectPath = Resolve-Path -Path (Join-Path $PSScriptRoot "..\BusinessObjects")
if (-not $projectPath) { throw "BusinessObjects project folder not found." }
$absModelsFolder = $modelsFolder
if (-not (Test-Path $absModelsFolder)) {
    New-Item -ItemType Directory -Path $absModelsFolder -Force | Out-Null
}

# Remove compiled artifacts to avoid stale assemblies referencing missing model types
$boBin = Join-Path $projectPath.ProviderPath "bin"
$boObj = Join-Path $projectPath.ProviderPath "obj"
if (Test-Path $boBin) { Remove-Item -Recurse -Force $boBin }
if (Test-Path $boObj) { Remove-Item -Recurse -Force $boObj }

# Use the Pomelo MySQL provider (project already references Pomelo)
$provider = 'Pomelo.EntityFrameworkCore.MySql'
# Restore packages for the BusinessObjects project so EF can retrieve project metadata
dotnet restore "$($projectPath.ProviderPath)"
# Ensure a startup project is provided to dotnet-ef so it can load design-time services
$startupProj = (Resolve-Path -Path (Join-Path $PSScriptRoot "..\CapstoneProject_BE\capstone_be.csproj")).ProviderPath
# Use --no-build to avoid build-time failures when current source lacks generated model files.
dotnet ef dbcontext scaffold "$conn" $provider --project "$($projectPath.ProviderPath)" --startup-project "$startupProj" -o "$absModelsFolder" -c FctmsContext -f --data-annotations --no-build
Write-Host "|-------------------------------------------|"
Write-Host "|-------Database scaffolding completed!-----|"
Write-Host "|-------------------------------------------|"
# Locate the generated DbContext file. Preferred name is FctmsContext.cs but
# EF may generate a different name; search for any *Context.cs as fallback.
$dbContextPath = Join-Path $absModelsFolder "FctmsContext.cs"
Write-Host "Looking for DbContext at: $dbContextPath"
if (-not (Test-Path $dbContextPath)) {
    $found = Get-ChildItem -Path $absModelsFolder -Filter "*Context.cs" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $dbContextPath = $found.FullName
        Write-Host "Found DbContext at: $dbContextPath"
    }
    else {
        throw "DbContext file not found in '$absModelsFolder'. Scaffold may have failed. Check dotnet ef output."
    }
}

if (Test-Path $dbContextPath) {
    $text = Get-Content -Raw -Path $dbContextPath
    # Remove the OnConfiguring override method (handles block and expression-bodied forms, and optional preprocessor warnings)
    $pattern = '(?ms)protected\s+override\s+void\s+OnConfiguring\s*\([^)]*\)\s*(?:\{.*?\n\s*\}|(?:#.*\n\s*)*=>.*?;)'
    $newText = [regex]::Replace($text, $pattern, '')
    # Remove any remaining UseSqlServer(...) fragments that may span lines
    $newText = [regex]::Replace($newText, '(?ms).*UseSqlServer\(.+?\);', '')
    # Also remove any stray fragments that start with 'Database=' (case-insensitive) left over
    $newText = [regex]::Replace($newText, '(?mi)^\s*Database=.*$', '')
    # Remove any provider Use...MySql(...) calls that include ServerVersion.Parse(...) spanning lines
    $newText = [regex]::Replace($newText, '(?ms)Use\w*MySql\s*\(.+?ServerVersion\.Parse\(.+?\)\s*\)\s*;?', '')
    # Collapse multiple blank lines to a reasonable amount
    $newText = [regex]::Replace($newText, "(\r?\n){3,}", "`r`n`r`n")
    Start-Sleep -Seconds 1
    $newText | Out-File -FilePath $dbContextPath -Encoding UTF8 -Force
}

# Run a build to ensure generated models compile with the solution
Write-Host "Building solution to verify generated models..."
dotnet build (Join-Path $PSScriptRoot "..\capstone_be.sln")




