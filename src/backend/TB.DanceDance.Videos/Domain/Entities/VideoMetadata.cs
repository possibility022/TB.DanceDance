using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TB.DanceDance.Videos.Domain.Entities;

public class VideoMetadata
{
    private VideoMetadata() { }

    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public byte[] Metadata { get; set; } = null!;

    public static class Factory
    {
        public static VideoMetadata Create(Guid videoId, byte[] metadata)
            => new() { Id = Guid.NewGuid(), VideoId = videoId, Metadata = metadata };
    }
}

public class VideoMetadataConfiguration : IEntityTypeConfiguration<VideoMetadata>
{
    public void Configure(EntityTypeBuilder<VideoMetadata> builder)
    {
        builder.ToTable("VideoMetadata");
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