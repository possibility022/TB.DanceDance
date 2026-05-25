using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record CanUserUploadToEventRequest : IRequest<bool>
{
    public required string UserId { get; init; }
    public required Guid EventId { get; init; }
}

public record CanUserUploadToGroupRequest : IRequest<bool>
{
    public required string UserId { get; init; }
    public required Guid GroupId { get; init; }
}