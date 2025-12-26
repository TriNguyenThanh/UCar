# UCar Docker Setup

## Prerequisites
- Docker Desktop installed
- .NET 9.0 SDK (for local development)

## Quick Start

### 1. Start the services
```bash
docker-compose up -d
```

### 2. Check service status
```bash
docker-compose ps
```

### 3. View logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f ucar-app
docker-compose logs -f sqlserver
```

### 4. Access the application
- Application: http://localhost:5000
- SQL Server: localhost:1439
  - Username: sa
  - Password: YourStrong@Password123

## Apply Database Migrations

### Using EF Core CLI
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### Inside Docker container
```bash
docker-compose exec ucar-app dotnet ef database update
```

## Useful Commands

### Stop services
```bash
docker-compose down
```

### Stop and remove volumes (clean database)
```bash
docker-compose down -v
```

### Rebuild and restart
```bash
docker-compose up -d --build
```

### Execute SQL commands
```bash
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Password123
```

## Configuration

### SQL Server
- Port: 1439
- Default database: UCarDB
- Data persistence: Volume `sqlserver_data`

### Application
- Port: 5000 (mapped to container port 8080)
- Environment: Development
- Auto-restart: unless-stopped

## Security Notes

⚠️ **IMPORTANT**: Change the default SA password before deploying to production!

Update in:
- docker-compose.yml (sqlserver environment)
- appsettings.json (ConnectionStrings)
- appsettings.Development.json (ConnectionStrings)

## Troubleshooting

### SQL Server not ready
Wait for healthcheck to pass:
```bash
docker-compose logs sqlserver
```

### Connection refused
Ensure SQL Server is healthy:
```bash
docker-compose ps
```

### Reset everything
```bash
docker-compose down -v
docker-compose up -d --build
```
