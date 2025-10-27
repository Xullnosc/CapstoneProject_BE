
# Read connection string from config file
function Get-ConnectionString {
    param (
        [string]$devPath,
        [string]$defaultPath,
        [string]$key = "capstoneDb"
    )

    if (Test-Path $devPath) {
        $config = Get-Content -Raw -Path $devPath | ConvertFrom-Json
        return $config.ConnectionStrings.$key
    }
    elseif (Test-Path $defaultPath) {
        $config = Get-Content -Raw -Path $defaultPath | ConvertFrom-Json
        return $config.ConnectionStrings.$key
    }
    else {
        Write-Error "No configuration file found!"
        exit 1
    }
}

# Run Flyway migrations
function Run-Flyway {
    param (
        [string]$settingsPath
    )
    $appSettings = Get-Content  $settingsPath -Raw | ConvertFrom-Json

    $connString = $appSettings.ConnectionStrings.capstoneDb
    $server   = ($connString -replace ".*Server=([^;]+);.*", '$1')
    $database = ($connString -replace ".*Database=([^;]+);.*", '$1')
    $user     = ($connString -replace ".*User Id=([^;]+);.*", '$1')
    $password = ($connString -replace ".*Password=([^;]+);.*", '$1')
    $jdbcUrl = "jdbc:sqlserver://$server;databaseName=$database;encrypt=false;trustServerCertificate=true"

# Run Flyway migrate
    flyway -url="$jdbcUrl" -user="$user" -password="$password" -locations=filesystem:./BusinessObjects/db/migrations migrate
}