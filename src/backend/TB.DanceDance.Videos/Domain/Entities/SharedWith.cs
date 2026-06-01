using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Domain.Entities;
public class SharedWith
{
    private SharedWith() { }
    
    public Guid Id { get; set; }
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public Guid? EventId { get; set; }
    public Guid? GroupId { get; set; }

    public Video Video { get; set; } = null!;

    public class Factory
    {
        public static SharedWith Create(string userId, Guid? eventId, Guid? groupId)
        {
            return new SharedWith()
            {
                VideoId = Guid.Empty, // should be set by EF
                UserId = userId,
                EventId = eventId,
                GroupId = groupId
            };
        }
    }
}

public class SharedWithConfiguration : IEntityTypeConfiguration<SharedWith>
{
    public void Configure(EntityTypeBuilder<SharedWith> builder)
    {
        builder.ToTable("SharedWith", SchemaNames.Sharing);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.VideoId).IsRequired();

        builder.HasOne(x => x.Video)
            .WithMany(x => x.SharedWith)
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // EventId/GroupId/UserId reference entities owned by other modules, so no FK constraints
        // are declared here, but the columns are indexed for lookups (matching the original schema).
        builder.HasIndex(x => x.VideoId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.GroupId);
    }
}