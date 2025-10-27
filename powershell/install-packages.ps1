# should only be run when haven't installed the packages
# run in the project folder directory
#Need update: 
$packages = @(
    "Microsoft.EntityFrameworkCore.SqlServer",
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.Extensions.Configuration",
    "Microsoft.Extensions.Configuration.Json"
)

foreach ($pkg in $packages) {
    Write-Host "Installing $pkg..."
    dotnet add package $pkg --version 9.0.6
}

Write-Host "installed Nuget packages"