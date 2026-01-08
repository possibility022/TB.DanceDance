namespace Domain.Entities;

/// <summary>
/// Defines who can see comments on a video.
/// </summary>
public enum CommentVisibility
{
    /// <summary>
    /// Only authenticated (logged-in) users with a shared link can see comments.
    /// </summary>
    AuthenticatedOnly = 0,

    /// <summary>
    /// Only the video owner can see comments. Comments are private to the owner.
    /// </summary>
    OwnerOnly = 1,
    
    /// <summary>
    /// Anyone with any shared link can see comments (including anonymous users).
    /// </summary>
    Public = 2,
}
