using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Mobile.Data;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options)
    {

    }

    public DbSet<VideosToUpload> VideosToUpload { get; set; }
}