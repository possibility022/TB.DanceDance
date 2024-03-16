using Application;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DanceDbContext : DbContext, IApplicationContext
{
    public DanceDbContext(DbContextOptions<DanceDbContext> dbContextOptions) : base(dbContextOptions)
    {

    }

    public static class Schemas
    {
        public const string Access = "access";
        public const string Video = "video";
    }

    public DbSet<GroupAdmin> GroupsAdmins { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
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

        modelBuilder.Entity<GroupAdmin>()
            .ToTable("GroupsAdmins", Schemas.Access);

        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.Owner)
            .IsRequired()
            ;

        modelBuilder.Entity<AssignedToGroup>()
            .ToTable(nameof(AssingedToGroups), Schemas.Access);

        modelBuilder.Entity<AssignedToEvent>()
            .ToTable(nameof(AssingedToEvents), Schemas.Access);


        modelBuilder.Entity<EventAssigmentRequest>()
            .HasOne<Event>()
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .IsRequired();

        modelBuilder.Entity<EventAssigmentRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);

        modelBuilder.Entity<GroupAssigmentRequest>()
            .HasOne<Group>()
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .IsRequired();
        
        modelBuilder.Entity<GroupAssigmentRequest>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.ManagedBy);

        modelBuilder.Entity<VideoMetadata>()
            .HasOne<Video>()
            .WithMany()
            .HasForeignKey(e => e.VideoId)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<User>()
            .ToTable("Users", Schemas.Access);

        base.OnModelCreating(modelBuilder);
    }

}
