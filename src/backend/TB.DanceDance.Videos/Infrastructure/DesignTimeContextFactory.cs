using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TB.DanceDance.Videos.Infrastructure;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<VideosDbContext>
{
    public VideosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VideosDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Userid=postgres;Password=rgFraWIuyxONqWCQ71wh;Database=dancedance");

        return new VideosDbContext(optionsBuilder.Options);
    }
}