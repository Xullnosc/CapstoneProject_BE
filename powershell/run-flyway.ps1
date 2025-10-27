. (Join-Path $PSScriptRoot "..\powershell\environment-variables.ps1")
. (Join-Path $PSScriptRoot "..\powershell\functions.ps1")
# Read JSON
Run-Flyway -settingsPath $settingsPath

