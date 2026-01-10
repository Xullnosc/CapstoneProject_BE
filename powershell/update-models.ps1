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

dotnet ef dbcontext scaffold "$conn" Microsoft.EntityFrameworkCore.SqlServer --project "$($projectPath.ProviderPath)" -o "$absModelsFolder" -c FctmsContext -f --data-annotations
Write-Host "|-------------------------------------------|"
Write-Host "|-------Database scaffolding completed!-----|"
Write-Host "|-------------------------------------------|"
$dbContextPath = Join-Path $modelsFolder "FctmsContext.cs"
Write-Host $dbContextPath
if (Test-Path $modelsFolder) {
    $text = Get-Content -Raw -Path $dbContextPath
    # Remove the OnConfiguring override method (handles block and expression-bodied forms, and optional preprocessor warnings)
    $pattern = '(?ms)protected\s+override\s+void\s+OnConfiguring\s*\([^)]*\)\s*(?:\{.*?\n\s*\}|(?:#.*\n\s*)*=>.*?;)'
    $newText = [regex]::Replace($text, $pattern, '')
    # Remove any remaining UseSqlServer(...) fragments that may span lines
    $newText = [regex]::Replace($newText, '(?ms).*UseSqlServer\(.+?\);', '')
    # Also remove any stray fragments that start with 'Database=' left over
    $newText = [regex]::Replace($newText, '(?m)^\s*Database=.*$', '')
    # Collapse multiple blank lines to a reasonable amount
    $newText = [regex]::Replace($newText, "(\r?\n){3,}", "`r`n`r`n")
    Start-Sleep -Seconds 1
    $newText | Out-File -FilePath $dbContextPath -Encoding UTF8 -Force
}




