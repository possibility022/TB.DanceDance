using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.SharedKernel;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Infrastructure.EntitiesConfigurations;

public class SharedWithConfiguration : IEntityTypeConfiguration<SharedWith>
{
    public void Configure(EntityTypeBuilder<SharedWith> builder)
    {
        builder.ToTable("SharedWith", Constants.DbSchemas.Video);
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
