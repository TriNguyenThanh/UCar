using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace UCar.Data;

public class UCarDbContextFactory : IDesignTimeDbContextFactory<UCarDbContext>
{
    public UCarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UCarDbContext>();
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString(DbConstants.ConnectionStringName);
        optionsBuilder.UseSqlServer(connectionString);

        return new UCarDbContext(optionsBuilder.Options);
    }
}
