[CmdletBinding()]
param(
    [ValidateSet('repair', 'revert')]
    [string]$Action,

    [string]$settingsPath,

    [ValidateSet('clean-and-migrate-target', 'undo')]
    [string]$RevertMode = 'clean-and-migrate-target',

    [string]$TargetVersion,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Dot-source environment values when available.
$envFile = Join-Path $PSScriptRoot 'environment-variables.ps1'
if (Test-Path $envFile) {
    . $envFile
}

# Dot-source shared helpers.
$funcFile = Join-Path $PSScriptRoot 'functions.ps1'
if (-not (Test-Path $funcFile)) {
    throw "Required helper script not found: $funcFile"
}
. $funcFile

function Resolve-SettingsPath {
    param(
        [string]$ProvidedPath
    )

    if ($ProvidedPath) {
        if (-not (Test-Path $ProvidedPath)) {
            throw "settingsPath not found: $ProvidedPath"
        }
        return (Resolve-Path $ProvidedPath).ProviderPath
    }

    $candidates = @(
        (Join-Path $PSScriptRoot '..\CapstoneProject_BE\appsettings.Development.json'),
        (Join-Path $PSScriptRoot '..\CapstoneProject_BE\appsettings.json'),
        (Join-Path $PSScriptRoot '..\appsettings.Development.json'),
        (Join-Path $PSScriptRoot '..\appsettings.json')
    )

    $found = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $found) {
        throw 'Could not find an appsettings file. Provide -settingsPath explicitly.'
    }

    return (Resolve-Path $found).ProviderPath
}

function Get-FlywayBaseArgs {
    param(
        [string]$SettingsFilePath
    )

    $connString = Get-ConnectionString -SearchPaths @($SettingsFilePath) -Key 'capstoneDb'

    $parts = $connString -split ';' | Where-Object { $_ -match '=' }
    $parsedValues = @{}
    foreach ($part in $parts) {
        $key, $value = $part -split '=', 2
        $parsedValues[$key.Trim()] = $value.Trim()
    }

    $server = $parsedValues['Server']
    if (-not $server) { $server = $parsedValues['Host'] }
    if (-not $server) { $server = $parsedValues['Data Source'] }
    $database = $parsedValues['Database']
    $user = $parsedValues['User']
    if (-not $user) { $user = $parsedValues['User Id'] }
    if (-not $user) { $user = $parsedValues['UID'] }
    $password = $parsedValues['Password']
    if (-not $password) { $password = $parsedValues['PWD'] }

    if (-not $server) { throw 'Server could not be parsed from connection string.' }
    if (-not $database) { throw 'Database could not be parsed from connection string.' }

    $port = $parsedValues['Port']
    if (-not $port) { $port = '3306' }

    if ($server -notmatch ':\d+$') {
        $server = "${server}:${port}"
    }

    # Use ${} delimiters so PowerShell does not treat '?useSSL' as part of the variable token.
    $jdbcUrl = "jdbc:mysql://${server}/${database}?useSSL=false&serverTimezone=UTC&allowPublicKeyRetrieval=true"

    $repoRoot = (Resolve-Path -Path (Join-Path $PSScriptRoot '..')).ProviderPath
    $migrationLocation = Join-Path $repoRoot 'BusinessObjects\db\migrations'

    $args = @('-url="' + $jdbcUrl + '"')
    if ($user) { $args += ('-user="' + $user + '"') }
    if ($password) { $args += ('-password="' + $password + '"') }
    $args += ('-locations=filesystem:' + ($migrationLocation -replace '\\', '/'))

    # Keep consistent behavior with existing run-flyway wrapper.
    $args += '-baselineOnMigrate=true'

    return $args
}

function Invoke-FlywayCommand {
    param(
        [string]$SettingsFilePath,
        [string]$Command,
        [string[]]$ExtraArgs = @()
    )

    $flywayArgs = Get-FlywayBaseArgs -SettingsFilePath $SettingsFilePath
    if ($ExtraArgs.Count -gt 0) {
        $flywayArgs += $ExtraArgs
    }
    $flywayArgs += $Command

    $displayArgs = @()
    foreach ($arg in $flywayArgs) {
        if ($arg -match '^-password=') {
            $displayArgs += '-password="***"'
        }
        else {
            $displayArgs += $arg
        }
    }

    Write-Host "Running flyway with: $($displayArgs -join ' ')"
    & flyway @flywayArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Flyway command failed with exit code $LASTEXITCODE"
    }
}

$resolvedSettingsPath = Resolve-SettingsPath -ProvidedPath $settingsPath

switch ($Action) {
    'repair' {
        Invoke-FlywayCommand -SettingsFilePath $resolvedSettingsPath -Command 'repair'
    }

    'revert' {
        if ($RevertMode -eq 'undo') {
            $undoArgs = @()
            if ($TargetVersion) {
                $undoArgs += "-target=$TargetVersion"
            }

            try {
                Invoke-FlywayCommand -SettingsFilePath $resolvedSettingsPath -Command 'undo' -ExtraArgs $undoArgs
            }
            catch {
                throw "Undo failed. If you are using Flyway Community (no undo support), use -RevertMode clean-and-migrate-target with -TargetVersion and -Force. Details: $($_.Exception.Message)"
            }
        }
        else {
            if (-not $TargetVersion) {
                throw 'TargetVersion is required when using -RevertMode clean-and-migrate-target.'
            }

            if (-not $Force) {
                throw 'Revert via clean is destructive. Re-run with -Force to confirm.'
            }

            Write-Warning 'About to run flyway clean (destructive) and then migrate to target version.'
            Invoke-FlywayCommand -SettingsFilePath $resolvedSettingsPath -Command 'clean' -ExtraArgs @('-cleanDisabled=false')
            Invoke-FlywayCommand -SettingsFilePath $resolvedSettingsPath -Command 'migrate' -ExtraArgs @("-target=$TargetVersion")
        }
    }
}
