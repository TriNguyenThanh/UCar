Write-Host "</>: Removing existing Migrations folder" -ForegroundColor Cyan
if (Test-Path Migrations) { 
    Remove-Item -Recurse -Force Migrations -ErrorAction SilentlyContinue
}
Write-Host "</>: Removed existing Migrations folder successfully" -ForegroundColor Green

Write-Host "</>: Dropping existing database..." -ForegroundColor Cyan
dotnet ef database drop --force
if ($LASTEXITCODE -ne 0) {
    Write-Host "</>: Warning - Failed to drop database (might not exist)" -ForegroundColor Yellow
}
Write-Host "</>: Database dropped successfully" -ForegroundColor Green

Write-Host "</>: Creating new initial migration..." -ForegroundColor Cyan
dotnet ef migrations add Initdb
if ($LASTEXITCODE -ne 0) {
    Write-Host "</>: Failed to create migration" -ForegroundColor Red
    exit 1
}
Write-Host "</>: Created new initial migration successfully" -ForegroundColor Green

Write-Host "</>: Updating database..." -ForegroundColor Cyan
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "</>: Failed to update database" -ForegroundColor Red
    exit 1
}
Write-Host "</>: Database updated successfully" -ForegroundColor Green