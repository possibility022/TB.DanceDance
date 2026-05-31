using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Creates a brand-new <c>Video</c> with its initial <c>SharedWith</c> and returns an upload SAS.
/// This is the create-new-video + first-SAS path — distinct from <see cref="CreateSharingLinkCommand"/>,
/// which re-issues an upload SAS for an already existing video.
/// </summary>
public record CreateVideoUploadCommand : IRequest<UploadContext?>
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public required SharingWithType SharingWithType { get; init; }
    public Guid? SharedWith { get; init; }
}
