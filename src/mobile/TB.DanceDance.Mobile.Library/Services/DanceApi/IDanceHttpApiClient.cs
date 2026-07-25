using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Sharing;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public interface IDanceHttpApiClient
{
    Task RenameVideoAsync(Guid videoId, string newName, CancellationToken cancellationToken = default);
    Task DeleteVideoAsync(Guid videoId, CancellationToken cancellationToken = default);
    Task<GetUserAccessResponse> GetUserAccesses(CancellationToken cancellationToken = default);
    Task RequestAccess(RequestAccessRequest accessRequest, CancellationToken cancellationToken = default);
    Task<PagedResponse<VideoFromGroupInformation>> GetVideosFromGroups(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<VideoInformation>> GetVideosForEvent(Guid eventId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RefreshUploadUrlResponse> RefreshUploadUrl(
        Guid videoId,
        CancellationToken cancellationToken = default);
    Task<ProduceUploadUrlResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
        DateTime recordedTimeUtc,
        CancellationToken cancellationToken = default
    );
    Task<Stream> GetStream(string videoBlobId, CancellationToken cancellationToken = default);
    Task<(Uri uri, string authToken)> GetVideoUri(string videoBlobId, CancellationToken cancellationToken = default);
    Task CreateEvent(string eventName, DateTime eventDate, CancellationToken cancellationToken = default);
    Task<PagedResponse<VideoInformation>> GetMyVideos(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SharedLinkResponse?> GetSharingLinkAsync(Guid videoId, CancellationToken token = default);
    Task RevokeShareLinkAsync(string linkId, CancellationToken token = default);
}
