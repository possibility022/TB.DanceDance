using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record RenameVideoCommand : IRequest<bool>
{
    public required Guid VideoId { get; set; }
    public required string NewName { get; set; }
}