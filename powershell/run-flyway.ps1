# Parameter: optional settings path and command
param(
	[string]$settingsPath,
    [string]$command = "migrate"
)
# Dot-source environment-variables only if it exists
$envFile = Join-Path $PSScriptRoot "environment-variables.ps1"
if (Test-Path $envFile) {
	. $envFile
}
else {
	Write-Verbose "No environment-variables.ps1 found at $envFile; continuing without it."
}

# Dot-source functions; fail fast if missing
$funcFile = Join-Path $PSScriptRoot "functions.ps1"
if (Test-Path $funcFile) {
	. $funcFile
}
else {
	throw "Required helper script not found: $funcFile"
}

# Allow optional -settingsPath parameter; if provided pass it through
if ($settingsPath) {
	Run-Flyway -settingsPath $settingsPath -command $command
}
else {
	Run-Flyway -command $command
}

