namespace Domain.Entities;

/// <summary>
/// Represents a comment on a video, either from an authenticated user or an anonymous user via a shared link.
/// </summary>
public class Comment
{
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
    public string? AnonymouseName { get; set; }

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
    public User? User { get; set; }
    public Video Video { get; set; } = null!;
    public SharedLink? SharedLink { get; set; }
}
