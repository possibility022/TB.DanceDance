using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.SharedKernel;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Infrastructure.EntitiesConfigurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments", Constants.DbSchemas.Comments);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // UserId is null for anonymous comments; UpdatedAt is null until the comment is edited.
        // Neither may be marked required.
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.AnonymousName).HasMaxLength(20);
        builder.Property(x => x.ShaOfAnonymousId).HasMaxLength(32);

        builder.HasOne(x => x.Video)
            .WithMany()
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(x => x.SharedLink)
            .WithMany()
            .HasForeignKey(x => x.SharedLinkId)
            .IsRequired(false);

        builder.HasIndex(x => x.VideoId);
        builder.HasIndex(x => x.SharedLinkId);
        builder.HasIndex(x => x.UserId);
    }
}
