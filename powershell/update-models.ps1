# to update models by deleting them first and then rebuild the models folder from scratch
# run in the project folder directory
. (Join-Path $PSScriptRoot "..\powershell\environment-variables.ps1")
. (Join-Path $PSScriptRoot "..\powershell\functions.ps1")
Write-Host $devConfigPath
$conn = Get-ConnectionString -devPath ($devConfigPath) -defaultPath ($defaultConfigPath)

if (Test-Path ($modelsFolder)) {
    Remove-Item -Recurse -Force ($modelsFolder)
    Write-Host "|-------------------------------------------|"
    Write-Host "|---------- Old models deleted -------------|"
    Write-Host "|-------------------------------------------|"
}


Write-Host $conn
dotnet ef dbcontext scaffold $conn Microsoft.EntityFrameworkCore.SqlServer --project (Join-Path $PSScriptRoot "..\BusinessObjects") -o $modelsFolder -c CapstoneDbContext -f --data-annotations
Write-Host "|-------------------------------------------|"
Write-Host "|-------Database scaffolding completed!-----|"
Write-Host "|-------------------------------------------|"
$dbContextPath = Join-Path $modelsFolder "CapstoneDbContext.cs"
Write-Host $dbContextPath
if (Test-Path $modelsFolder) {
    $content = Get-Content $dbContextPath
    $startLineIndex = -1

    for ($i = 0; $i -lt $content.Count; $i++) {
        if ($content[$i] -match 'protected override void OnConfiguring') {
            $startLineIndex = $i
            break
        }
    }
    if ($startLineIndex -ne -1) {
        $newContent = New-Object System.Collections.Generic.List[string]
        for ($i = 0; $i -lt $content.Count; $i++) {
            if ($i -lt $startLineIndex -or $i -gt ($startLineIndex + 2)) {
                $newContent.Add($content[$i])
            }
        }
        Start-Sleep -Seconds 5
        $newContent | Out-File -FilePath $dbContextPath -Encoding UTF8 -Force
    }
}




