using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Domain.Entities;

/// <summary>
/// Represents a comment on a video, either from an authenticated user or an anonymous user via a shared link.
/// </summary>
public class Comment
{
    private Comment() { }
    
    public Guid Id { get; set; }

    /// <summary>
    /// The video this comment belongs to.
    /// </summary>
    public Guid VideoId { get; set; }

    /// <summary>
    /// The authenticated user who created this comment. Null for anonymous comments.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The shared link used to create this comment (for anonymous comments). Null for authenticated comments.
    /// </summary>
    public string? SharedLinkId { get; set; }

    /// <summary>
    /// The comment content.
    /// </summary>
    public string Content { get; set; } = null!;
    
    /// <summary>
    /// True when posted as anonymouse
    /// </summary>
    public bool PostedAsAnonymous { get; set; }

    /// <summary>
    /// Anonymous name if posted as anonymouse
    /// </summary>
    public string? AnonymousName { get; set; }
    
    /// <summary>
    /// This is a hashed ID generated on frontend used to allow edit comments posted anonymously.
    /// It is used to identify the comment owner when the comment was posted by/as anonymouse user.
    /// </summary>
    public byte[]? ShaOfAnonymousId { get; set; }
    
    /// <summary>
    /// When the comment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the comment was last updated. Null if never updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Whether the comment is hidden. Hidden comments are only visible to the video owner.
    /// </summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Whether the comment has been reported as inappropriate.
    /// </summary>
    public bool IsReported { get; set; } = false;

    /// <summary>
    /// The reason provided when the comment was reported. Null if not reported.
    /// </summary>
    public string? ReportedReason { get; set; }

    // Navigation properties
    public Video Video { get; set; } = null!;
    public SharedLink? SharedLink { get; set; }

    /// <summary>
    /// Creates comments inside the Videos module (the ctor is private, like the other entities here).
    /// </summary>
    public static class Factory
    {
        public static Comment Create(
            Guid videoId,
            string? userId,
            string sharedLinkId,
            string content,
            string? anonymousName,
            byte[]? shaOfAnonymousId) => new()
        {
            Id = Guid.NewGuid(),
            VideoId = videoId,
            UserId = userId, // null for anonymous, populated for authenticated
            SharedLinkId = sharedLinkId,
            Content = content,
            AnonymousName = anonymousName,
            ShaOfAnonymousId = shaOfAnonymousId,
            PostedAsAnonymous = userId is null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            IsHidden = false,
            IsReported = false,
            ReportedReason = null,
        };
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments", SchemaNames.Comments);
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

