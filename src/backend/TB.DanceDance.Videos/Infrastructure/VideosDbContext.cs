using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Infrastructure;

public class VideosDbContext : DbContext
{
    public VideosDbContext(DbContextOptions<VideosDbContext> options) : base(options) { }


    public DbSet<Video> Videos { get; set; }
    public DbSet<VideoMetadata> VideoMetadata { get; set; }
    public DbSet<SharedWith> SharedWith { get; set; }   // GroupId/EventId/UserId as plain Guid — no navigation
    public DbSet<SharedLink> SharedLinks { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(o => o.MigrationsHistoryTable("Video_MigrationHistory"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema(SchemaNames.Video);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VideosDbContext).Assembly);
    }
}