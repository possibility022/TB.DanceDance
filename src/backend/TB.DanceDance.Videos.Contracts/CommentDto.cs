namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// A comment on a video. Carries the raw persisted fields; view-level concerns
/// (resolving the authenticated author's display name, computing IsOwn / CanModerate)
/// are handled at the API edge in Task 06, since the Videos module cannot navigate to
/// the Access module's User entity.
/// </summary>
public record CommentDto
{
    public Guid Id { get; init; }
    public Guid VideoId { get; init; }

    /// <summary>The authenticated user who authored the comment. Null for anonymous comments.</summary>
    public string? UserId { get; init; }

    public string Content { get; init; } = null!;

    public bool PostedAsAnonymous { get; init; }

    /// <summary>Author display name supplied when posting anonymously. Null for authenticated comments.</summary>
    public string? AnonymousName { get; init; }

    /// <summary>SHA-256 hash of the client-side anonymous id, used to identify an anonymous author's own comments.</summary>
    public byte[]? ShaOfAnonymousId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public bool IsHidden { get; init; }
    public bool IsReported { get; init; }
    public string? ReportedReason { get; init; }

    /// <summary>The id of the user who owns the video this comment belongs to (for moderation checks at the edge).</summary>
    public string VideoOwnerId { get; init; } = null!;
}
