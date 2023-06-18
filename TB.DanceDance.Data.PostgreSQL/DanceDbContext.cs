using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Data.PostgreSQL;

public class DanceDbContext : DbContext
{
    public DanceDbContext(DbContextOptions<DanceDbContext> dbContextOptions) : base(dbContextOptions)
    {
        
    }

    public static class Schemas
    {
        public const string Access = "access";
        public const string Video = "video";
    }

    public DbSet<Video> Videos { get; set; }
    public DbSet<VideoToTranform> VideosToTranform { get; set; }
    public DbSet<VideoMetadata> VideoMetadata { get; set; }
    public DbSet<GroupAssigmentRequest> GroupAssigmentRequests { get; set; }
    public DbSet<EventAssigmentRequest> EventAssigmentRequests { get; set; }
    public DbSet<SharedWith> SharedWith { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<AssignedToGroup> AssingedToGroups { get; set; }
    public DbSet<AssignedToEvent> AssingedToEvents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>()
            .ToTable("Videos", Schemas.Video);

        modelBuilder.Entity<VideoToTranform>()
            .ToTable("ToTransform", Schemas.Video);

        modelBuilder.Entity<VideoMetadata>()
            .ToTable(nameof(VideoMetadata), Schemas.Video);

        modelBuilder.Entity<GroupAssigmentRequest>()
            .ToTable(nameof(GroupAssigmentRequests), Schemas.Access);

        modelBuilder.Entity<EventAssigmentRequest>()
            .ToTable(nameof(EventAssigmentRequests), Schemas.Access);

        modelBuilder.Entity<SharedWith>()
            .ToTable(nameof(SharedWith), Schemas.Access);

        modelBuilder.Entity<Group>()
            .ToTable("Groups", Schemas.Access);

        modelBuilder.Entity<Event>()
            .ToTable("Events", Schemas.Access);

        modelBuilder.Entity<AssignedToGroup>()
            .ToTable(nameof(AssingedToGroups), Schemas.Access);

        modelBuilder.Entity<AssignedToEvent>()
            .ToTable(nameof(AssingedToEvents), Schemas.Access);


        modelBuilder.Entity<EventAssigmentRequest>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .IsRequired();

        modelBuilder.Entity<GroupAssigmentRequest>()
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired();

        modelBuilder.Entity<VideoMetadata>()
            .HasOne<Video>()
            .WithMany()
            .HasForeignKey(e => e.VideoId)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }

}
