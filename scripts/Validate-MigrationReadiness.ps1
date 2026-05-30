param(
    [string]$Solution = "WMS.sln",
    [string]$ScriptPath = "App_Data/migration-idempotent.sql"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking build..."
dotnet build $Solution -v:minimal

Write-Host "Listing migrations..."
dotnet ef migrations list --no-build

Write-Host "Generating idempotent migration script..."
dotnet ef migrations script --idempotent --no-build -o $ScriptPath

Write-Host "Migration readiness script generated at $ScriptPath"
