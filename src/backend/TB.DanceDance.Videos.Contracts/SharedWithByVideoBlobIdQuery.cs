using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record SharedWithByVideoBlobIdQuery : IRequest<IReadOnlyCollection<SharedWithResponse>>
{
    public required string VideoBlobId { get; init; }
}
