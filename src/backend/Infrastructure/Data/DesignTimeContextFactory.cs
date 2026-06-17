using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<DanceDbContext>
{
    public DanceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DanceDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("TBDANCEDANCE_MIGRATION_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=dancedance";
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new DanceDbContext(optionsBuilder.Options);
    }
}
