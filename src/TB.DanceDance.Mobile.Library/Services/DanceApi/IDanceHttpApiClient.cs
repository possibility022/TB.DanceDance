using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public interface IDanceHttpApiClient
{
    Task RenameVideoAsync(Guid videoId, string newName);
    Task<UserEventsAndGroupsResponse> GetUserAccesses();
    Task RequestAccess(RequestAssigmentModelRequest accessRequest);
    Task<IReadOnlyCollection<GroupWithVideosResponse>?> GetVideosFromGroups();
    Task<IReadOnlyCollection<VideoInformationResponse>> GetVideosForEvent(Guid eventId);
    Task<UploadVideoInformationResponse> RefreshUploadUrl(Guid videoId);
    Task<UploadVideoInformationResponse?> GetUploadInformation(
        string fileName,
        string nameOfVideo,
        SharingWithType sharingWith,
        Guid? sharedWithId,
        DateTime recordedTimeUtc
    );
    Task<Stream> GetStream(string videoBlobId);
    Uri GetVideoUri(string videoBlobId);
    Task CreateEvent(string eventName, DateTime eventDate);
    Task<IReadOnlyCollection<VideoInformationResponse>> GetMyVideos();
}
