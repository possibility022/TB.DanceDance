using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.SharedKernel;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Infrastructure.EntitiesConfigurations;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos", Constants.DbSchemas.Video);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.UploadedBy).IsRequired();
        builder.Property(x => x.FileName).IsRequired();
        builder.Property(x => x.SourceBlobId).IsRequired();

        // The SharedWith relationship is owned by SharedWithConfiguration (dependent side).
    }
}
