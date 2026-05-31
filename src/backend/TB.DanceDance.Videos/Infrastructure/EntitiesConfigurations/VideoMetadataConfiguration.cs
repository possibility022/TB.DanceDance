using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.SharedKernel;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Infrastructure.EntitiesConfigurations;

public class VideoMetadataConfiguration : IEntityTypeConfiguration<VideoMetadata>
{
    public void Configure(EntityTypeBuilder<VideoMetadata> builder)
    {
        builder.ToTable("VideoMetadata", Constants.DbSchemas.Video);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VideoId).IsRequired();
        builder.Property(x => x.Metadata).IsRequired();

        // VideoMetadata has no navigation back to Video, so the FK is configured without one.
        builder.HasOne<Video>()
            .WithMany()
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(x => x.VideoId);
    }
}
