using Application;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DanceDbContext : DbContext, IApplicationContext
{
    public DanceDbContext(DbContextOptions<DanceDbContext> dbContextOptions) : base(dbContextOptions)
    {

    }

    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<VideoMetadata> VideoMetadata { get; set; }
    public DbSet<SharedWith> SharedWith { get; set; }
    public DbSet<SharedLink> SharedLinks { get; set; }
    public DbSet<Comment> Comments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>()
            .ToTable("Videos", Schemas.Video);

        modelBuilder.Entity<VideoMetadata>()
            .ToTable(nameof(VideoMetadata), Schemas.Video);
        
        modelBuilder.Entity<VideoMetadata>()
            .HasOne<Video>()
            .WithMany()
            .HasForeignKey(e => e.VideoId)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<User>()
            .ToTable("Users", Schemas.Access);

        modelBuilder.Entity<SharedLink>()
            .ToTable("SharedLinks", Schemas.Access);
        
        modelBuilder.Entity<SharedLink>()
            .HasOne<Video>(e => e.Video)
            .WithMany()
            .HasForeignKey(r => r.VideoId)
            .IsRequired();
        
        modelBuilder.Entity<SharedLink>()
            .HasOne<User>(e => e.SharedByUser)
            .WithMany()
            .HasForeignKey(r => r.SharedBy)
            .IsRequired();
        
        modelBuilder.Entity<SharedLink>()
            .HasIndex(r => r.Id)
            .IsUnique();

        // Comment configuration
        modelBuilder.Entity<Comment>()
            .ToTable("Comments", Schemas.Comments);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Video)
            .WithMany()
            .HasForeignKey(c => c.VideoId)
            .IsRequired();

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .IsRequired(false);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.SharedLink)
            .WithMany()
            .HasForeignKey(c => c.SharedLinkId)
            .IsRequired(false);

        modelBuilder.Entity<Comment>()
            .Property(c => c.Content)
            .HasMaxLength(2000)
            .IsRequired();
        
        modelBuilder.Entity<Comment>()
            .Property(c => c.AnonymousName)
            .HasMaxLength(20)
            .IsRequired(false);
        
        modelBuilder.Entity<Comment>()
            .Property(c => c.ShaOfAnonymousId)
            .HasMaxLength(32)
            .IsRequired(false);

        base.OnModelCreating(modelBuilder);
    }

}
