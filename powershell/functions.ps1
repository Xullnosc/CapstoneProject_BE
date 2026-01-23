
# Read connection string from config file
function Get-ConnectionString {
    param (
        [string[]]$SearchPaths,
        [string]$Key = "capstoneDb"
    )

    # If no explicit search paths were provided, build a list of common locations
    if (-not $SearchPaths -or $SearchPaths.Count -eq 0) {
        $root = Split-Path -Parent $PSScriptRoot
        # Build explicit string paths to avoid accidental arrays being passed to Join-Path
        $SearchPaths = @()
        $SearchPaths += Join-Path $root "CapstoneProject_BE\appsettings.Development.json"
        $SearchPaths += Join-Path $root "CapstoneProject_BE\appsettings.json"
        $SearchPaths += Join-Path $root "appsettings.Development.json"
        $SearchPaths += Join-Path $root "appsettings.json"
        $SearchPaths += Join-Path $root "BusinessObjects\appsettings.Development.json"
        $SearchPaths += Join-Path $root "BusinessObjects\appsettings.json"
        $SearchPaths += Join-Path $PSScriptRoot "appsettings.Development.json"
        $SearchPaths += Join-Path $PSScriptRoot "appsettings.json"
    }

    foreach ($path in $SearchPaths) {
        if (-not $path) { continue }
        try {
            if (Test-Path $path) {
                $json = Get-Content -Raw -Path $path | ConvertFrom-Json
                if ($null -ne $json.ConnectionStrings -and $null -ne $json.ConnectionStrings.$Key) {
                    return $json.ConnectionStrings.$Key
                }
                # Also support top-level key fallback
                if ($null -ne $json.$Key) {
                    return $json.$Key
                }
            }
        }
        catch {
            Write-Verbose ("Failed to read or parse {0}: {1}" -f $path, $_)
        }
    }

    throw "No configuration file with a connection string key '$Key' was found in searched locations."
}

# Run Flyway migrations
function Run-Flyway {
    param (
        [string]$settingsPath
    )
    # If settingsPath wasn't provided, try to locate one using Get-ConnectionString helper locations
    if (-not $settingsPath) {
        $candidates = @()
        $candidates += Join-Path $PSScriptRoot "..\CapstoneProject_BE\appsettings.Development.json"
        $candidates += Join-Path $PSScriptRoot "..\CapstoneProject_BE\appsettings.json"
        $candidates += Join-Path $PSScriptRoot "..\appsettings.Development.json"
        $candidates += Join-Path $PSScriptRoot "..\appsettings.json"
        $settingsPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }

    if (-not $settingsPath -or -not (Test-Path $settingsPath)) {
        throw "Could not find an appsettings file to read connection string from. Provide -settingsPath or add appsettings.json next to the solution."
    }

    $appSettings = Get-Content -Raw -Path $settingsPath | ConvertFrom-Json
    $connString = $null
    if ($appSettings.ConnectionStrings -and $appSettings.ConnectionStrings.capstoneDb) {
        $connString = $appSettings.ConnectionStrings.capstoneDb
    }
    elseif ($appSettings.capstoneDb) {
        $connString = $appSettings.capstoneDb
    }
    else {
        # Fallback: attempt to use Get-ConnectionString to search
        try { $connString = Get-ConnectionString -SearchPaths @($settingsPath) -Key 'capstoneDb' } catch {}
    }

    if (-not $connString) { throw "capstoneDb connection string not found in $settingsPath" }

    Write-Host "Connection String: $connString"
    
    # Parse connection string by splitting on semicolons
    $parts = $connString -split ';' | Where-Object { $_ -match '=' }
    $parsedValues = @{}
    foreach ($part in $parts) {
        $key, $value = $part -split '=', 2
        $parsedValues[$key.Trim()] = $value.Trim()
    }
    
    $server   = $parsedValues['Server'] + ':3306'
    $database = $parsedValues['Database']
    $user     = $parsedValues['User']
    if (-not $user) { $user = $parsedValues['User Id'] }
    $password = $parsedValues['Password']

    Write-Host "Parsed - Server: $server, Database: $database, User: $user, Password: (hidden)"

    if (-not $database) {
        throw "Database value could not be parsed from connection string: $connString"
    }

    $jdbcUrl = "jdbc:mysql://" + $server + "/" + $database + "?useSSL=false&serverTimezone=UTC&allowPublicKeyRetrieval=true"

    $repoRoot = (Resolve-Path -Path (Join-Path $PSScriptRoot ".." )).ProviderPath
    $migrationLocation = Join-Path $repoRoot "BusinessObjects\db\migrations"

    $flywayCmd = @('-url="' + $jdbcUrl + '"')
    if ($user -and $user -ne $connString) { $flywayCmd += ('-user="' + $user + '"') }
    if ($password -and $password -ne $connString) { $flywayCmd += ('-password="' + $password + '"') }
    $flywayCmd += ('-locations=filesystem:' + ($migrationLocation -replace '\\','/'))
    # When the database already contains schema objects but not Flyway metadata,
    # enable baseline on migrate so Flyway will record the current state instead
    # of attempting to re-run initial migrations that would fail.
    $flywayCmd += '-baselineOnMigrate=true'
    $flywayCmd += 'migrate'

    Write-Host "Running flyway with: $($flywayCmd -join ' ')"
    & flyway @flywayCmd
}