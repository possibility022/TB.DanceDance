using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record SharedWithByVideoIdQuery : IRequest<IReadOnlyCollection<SharedWithResponse>>
{
    public required Guid VideoId { get; init; }
}
