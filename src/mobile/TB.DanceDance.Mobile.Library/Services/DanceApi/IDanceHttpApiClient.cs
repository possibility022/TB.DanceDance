using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Sharing;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public interface IDanceHttpApiClient
{
    Task RenameVideoAsync(Guid videoId, string newName);
    Task<GetUserAccessResponse> GetUserAccesses();
    Task RequestAccess(RequestAccessRequest accessRequest);
    Task<IReadOnlyCollection<VideoFromGroupInformation>?> GetVideosFromGroups();
    Task<IReadOnlyCollection<VideoInformation>> GetVideosForEvent(Guid eventId);
    Task<RefreshUploadUrlResponse> RefreshUploadUrl(Guid videoId);
    Task<ProduceUploadUrlResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
        DateTime recordedTimeUtc
    );
    Task<Stream> GetStream(string videoBlobId);
    (Uri uri, string authToken) GetVideoUri(string videoBlobId);
    Task CreateEvent(string eventName, DateTime eventDate);
    Task<PagedResponse<VideoInformation>> GetMyVideos(int page, int pageSize);
    Task<SharedLinkResponse?> GetSharingLinkAsync(Guid videoId, CancellationToken token = default);
    Task RevokeShareLinkAsync(string linkId, CancellationToken token = default);
}
