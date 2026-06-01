using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Creates a comment on a video through a shared link. The video id is resolved from the link.
/// <paramref name="UserId"/> is null for anonymous comments; <paramref name="AnonymousId"/> is the
/// client-side identifier hashed to let an anonymous author manage their own comments later.
/// </summary>
public record CreateCommentCommand(
    string? UserId,
    string LinkId,
    string Content,
    string? AuthorName,
    string? AnonymousId) : IRequest<CommentDto>;

/// <summary>
/// Gets comments for a video accessed through a shared link. Applies the video's
/// CommentVisibility rules plus owner-sees-all and the requester's own/anonymous comments.
/// </summary>
public record GetCommentsForVideoByLinkQuery(
    string? UserId,
    string? AnonymousId,
    string LinkId) : IRequest<IReadOnlyCollection<CommentDto>>;

/// <summary>
/// Gets comments for a video the (authenticated) user already has access to. Runs the
/// cross-module access check first, then the same visibility rules as the link overload.
/// </summary>
public record GetCommentsForVideoByIdQuery(
    string UserId,
    string? AnonymousId,
    Guid VideoId) : IRequest<IReadOnlyCollection<CommentDto>>;

/// <summary>
/// Updates a comment. Allowed for the authenticated author or an anonymous author whose
/// hashed anonymous id matches.
/// </summary>
public record UpdateCommentCommand(
    Guid CommentId,
    string? UserId,
    string? AnonymousId,
    string? AuthorName,
    string Content) : IRequest<bool>;

/// <summary>
/// Deletes a comment. Allowed for the authenticated author, the video owner, or an anonymous
/// author whose hashed anonymous id matches.
/// </summary>
public record DeleteCommentCommand(
    Guid CommentId,
    string? UserId,
    string? AnonymousId) : IRequest<bool>;

/// <summary>Hides a comment. Video owner only.</summary>
public record HideCommentCommand(Guid CommentId, string VideoOwnerId) : IRequest<bool>;

/// <summary>Unhides a comment. Video owner only.</summary>
public record UnhideCommentCommand(Guid CommentId, string VideoOwnerId) : IRequest<bool>;

/// <summary>Reports a comment as inappropriate. Allowed for anyone.</summary>
public record ReportCommentCommand(Guid CommentId, string Reason) : IRequest<bool>;
