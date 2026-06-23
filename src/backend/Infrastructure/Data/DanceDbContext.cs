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
        public const string Comments = "comments";
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
    public DbSet<SharedLink> SharedLinks { get; set; }
    public DbSet<VideoTransfer> VideoTransfers { get; set; }
    public DbSet<VideoTransferItem> VideoTransferItems { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Competition> Competitions { get; set; }
    public DbSet<InviteLink> InviteLinks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>()
            .ToTable("Videos", Schemas.Video);

        // Competition configuration (lives in the video schema, alongside Video)
        modelBuilder.Entity<Competition>()
            .ToTable("Competitions", Schemas.Video);

        // A video belongs to at most one competition; deleting a competition detaches its
        // videos (sets CompetitionId null) rather than cascade-deleting them.
        modelBuilder.Entity<Video>()
            .HasOne(v => v.Competition)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CompetitionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<SharedLink>()
            .ToTable("SharedLinks", Schemas.Access, t => t.HasCheckConstraint(
                "CK_SharedLinks_VideoOrCompetition",
                "(\"VideoId\" IS NOT NULL) <> (\"CompetitionId\" IS NOT NULL)"));

        // A link targets either a single video or a competition. Both FKs are optional at the column
        // level (the check constraint enforces exactly-one), and deleting the target removes its links.
        modelBuilder.Entity<SharedLink>()
            .HasOne<Video>(e => e.Video)
            .WithMany()
            .HasForeignKey(r => r.VideoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharedLink>()
            .HasOne<Competition>(e => e.Competition)
            .WithMany()
            .HasForeignKey(r => r.CompetitionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SharedLink>()
            .HasOne<User>(e => e.SharedByUser)
            .WithMany()
            .HasForeignKey(r => r.SharedBy)
            .IsRequired();
        
        modelBuilder.Entity<SharedLink>()
            .HasIndex(r => r.Id)
            .IsUnique();

        // Video transfer configuration (same schema as SharedLink)
        modelBuilder.Entity<VideoTransfer>()
            .ToTable("VideoTransfers", Schemas.Access);

        modelBuilder.Entity<VideoTransfer>()
            .HasOne<User>(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .IsRequired();

        modelBuilder.Entity<VideoTransfer>()
            .HasIndex(r => r.Id)
            .IsUnique();

        modelBuilder.Entity<VideoTransferItem>()
            .ToTable("VideoTransferItems", Schemas.Access);

        modelBuilder.Entity<VideoTransferItem>()
            .HasOne<VideoTransfer>(e => e.Transfer)
            .WithMany(t => t.Items)
            .HasForeignKey(r => r.TransferId)
            .IsRequired();

        modelBuilder.Entity<VideoTransferItem>()
            .HasOne<Video>(e => e.Video)
            .WithMany()
            .HasForeignKey(r => r.VideoId)
            .IsRequired();

        modelBuilder.Entity<VideoTransferItem>()
            .HasIndex(r => new { r.TransferId, r.VideoId })
            .IsUnique();

        // Comment configuration
        modelBuilder.Entity<Comment>()
            .ToTable("Comments", Schemas.Comments, t => t.HasCheckConstraint(
                "CK_Comments_VideoOrCompetition",
                "(\"VideoId\" IS NOT NULL) <> (\"CompetitionId\" IS NOT NULL)"));

        // A comment belongs to either a single video or a competition's combined thread. Both FKs are
        // optional at the column level (the check constraint enforces exactly-one), and deleting the
        // target removes its comments.
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Video)
            .WithMany()
            .HasForeignKey(c => c.VideoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Competition)
            .WithMany()
            .HasForeignKey(c => c.CompetitionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

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

        // Invite link configuration (same schema as SharedLink/VideoTransfer)
        modelBuilder.Entity<InviteLink>()
            .ToTable("InviteLinks", Schemas.Access, t => t.HasCheckConstraint(
                "CK_InviteLinks_GroupOrEvent",
                "(\"GroupId\" IS NOT NULL) <> (\"EventId\" IS NOT NULL)"));

        // A link targets either a group or an event. Both FKs are optional at the column level
        // (the check constraint enforces exactly-one), and deleting the target removes its links.
        modelBuilder.Entity<InviteLink>()
            .HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(r => r.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InviteLink>()
            .HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InviteLink>()
            .HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .IsRequired();

        modelBuilder.Entity<InviteLink>()
            .HasOne(e => e.RedeemedByUser)
            .WithMany()
            .HasForeignKey(r => r.RedeemedByUserId)
            .IsRequired(false);

        base.OnModelCreating(modelBuilder);
    }

}
