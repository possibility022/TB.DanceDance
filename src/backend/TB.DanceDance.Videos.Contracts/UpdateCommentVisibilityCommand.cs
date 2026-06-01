using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Owner-only toggle of a video's comment visibility. Returns false if the video is not found
/// or the requesting user is not the owner. <see cref="CommentVisibility"/> is the integer value
/// of the <c>Videos.Domain.Entities.CommentVisibility</c> enum.
/// </summary>
public record UpdateCommentVisibilityCommand : IRequest<bool>
{
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public required int CommentVisibility { get; init; }
}
