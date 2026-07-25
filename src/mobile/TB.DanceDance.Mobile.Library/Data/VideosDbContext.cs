using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Library.Data;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options)
    {

    }

    public DbSet<VideosToUpload> VideosToUpload { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VideosToUpload>()
            .HasIndex(x => new { x.State, x.NextAttemptAtUtc });
    }
}