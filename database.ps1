if (Test-path Migrations) { rmdir /s /q Migrations}
dotnet ef migrations add Initdb
dotnet ef database update