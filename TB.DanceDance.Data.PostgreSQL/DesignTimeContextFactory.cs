using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TB.DanceDance.Data.PostgreSQL;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<DanceDbContext>
{
    public DanceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DanceDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=dancedance");

        return new DanceDbContext(optionsBuilder.Options);
    }
}
