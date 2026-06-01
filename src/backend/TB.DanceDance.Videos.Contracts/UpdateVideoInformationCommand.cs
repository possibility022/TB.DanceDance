using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record UpdateVideoInformationCommand : IRequest<bool>
{
    public required Guid VideoId { get; init; }
    public required TimeSpan Duration { get; init; }
    public required DateTime Recorded { get; init; }
    public byte[]? Metadata { get; init; }
}
