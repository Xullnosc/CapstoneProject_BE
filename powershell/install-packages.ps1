# should only be run when haven't installed the packages
# run in the project folder directory
#Need update: 
$packages = @(
    "Microsoft.EntityFrameworkCore",
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.Extensions.Configuration",
    "Microsoft.Extensions.Configuration.Json",
    "Microsoft.EntityFrameworkCore.Tools"
)
foreach ($pkg in $packages) {
    Write-Host "Installing $pkg..."
    dotnet add package $pkg --version 9.0.6
}
dotnet add package AutoMapper --version 15.0.1
dotnet add package Google.Apis.Auth --version 1.73.0
dotnet add package Microsoft.AspNetCore.OpenApi --version 8.0.8
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 9.0.0	

Write-Host "installed Nuget packages"