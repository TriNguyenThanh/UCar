if (Test-Path Migrations) { Remove-Item -Recurse -Force Migrations }
echo "</>: Removed existing Migrations folder"
dotnet ef migrations add Initdb
echo "</>: Created new initial migration"
dotnet ef database update