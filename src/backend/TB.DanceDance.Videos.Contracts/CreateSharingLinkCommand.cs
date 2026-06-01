using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

public record CreateSharingLinkCommand : IRequest<UploadContext?>
{
    public Guid VideoId { get; set; }
}